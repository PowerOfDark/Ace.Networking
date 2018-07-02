﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledHostedObjectBase : EntangledObjectBase
    {
        private readonly HashSet<PropertyDescriptor> _pendingUpdates = new HashSet<PropertyDescriptor>();
        protected object _sync = new object();

        public EntangledHostedObjectBase(Guid eid, InterfaceDescriptor i)
        {
            Eid = eid;
            Descriptor = i;
            Context = new EntanglementProviderContext();
            PropertyChanged += EntangledHostedObjectBase_PropertyChanged;
        }

        protected EntanglementProviderContext Context { get; }

        private void EntangledHostedObjectBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Descriptor.Properties.TryGetValue(e.PropertyName, out var prop))
            {
                lock (_sync)
                {
                    _pendingUpdates.Add(prop);
                }

                PushPropertyUpdates();
            }
        }

        protected UpdateProperties GetAllProperties(IConnection con)
        {
            var packet = new UpdateProperties {Updates = new List<PropertyData>(), Eid = Eid};
            using (var ms = new MemoryStream())
            {
                foreach (var prop in Descriptor.Properties)
                {
                    ms.SetLength(0);
                    con.Serializer.Serialize(prop.Value.Property.GetValue(this), ms);
                    packet.Updates.Add(new PropertyData
                    {
                        PropertyName = prop.Value.Property.Name,
                        SerializedData = ms.ToArray()
                    });
                }
            }

            return packet;
        }

        protected void PushPropertyUpdates()
        {
            UpdateProperties packet;
            IPayloadSerializer serializer;
            lock (_sync)
            {
                if (_pendingUpdates.Count == 0 || Context.All.Clients.Count == 0) return;
                serializer = Context.All.Clients.First().Serializer;
                packet = new UpdateProperties {Updates = new List<PropertyData>(_pendingUpdates.Count), Eid = Eid};
                using (var ms = new MemoryStream())
                {
                    foreach (var prop in _pendingUpdates)
                    {
                        ms.SetLength(0);
                        serializer.Serialize(prop.Property.GetValue(this), ms);
                        packet.Updates.Add(new PropertyData
                        {
                            PropertyName = prop.Property.Name,
                            SerializedData = ms.ToArray()
                        });
                    }
                }

                _pendingUpdates.Clear();
            }

            Context.All.Send(packet);
        }


        public void Execute(IRequestWrapper req)
        {
            var cmd = (ExecuteMethod) req.Request;

            //find the best overload
            var overload = Descriptor.FindOverload(cmd);
            lock (Context)
            {
                if (!Context.All.ContainsClient(req.Connection))
                {
                    req.SendResponse(new ExecuteMethodResult
                    {
                        ExceptionAdapter = new RemoteExceptionAdapter("Unauthorized")
                    });
                    return;
                }

                Context.Sender = req.Connection;
                RemoteExceptionAdapter exception = null;
                Task task = null;
                try
                {
                    var args = new object[overload.Parameters.Length];
                    for (var i = 0; i < args.Length; i++)
                        using (var ms = new MemoryStream(cmd.Arguments[i].SerializedData))
                        {
                            args[i] = req.Connection.Serializer.DeserializeType(overload.Parameters[i].Type, ms);
                        }

                    task = (Task) overload.Method.Invoke(this, cmd.Arguments.Length == 0 ? null : args);
                }
                catch (Exception e)
                {
                    exception = new RemoteExceptionAdapter("A remote task failed", e);
                }

                // HUGE HACK WARNING
                // we need to somehow operate on Task<T>, where T is unknown at compile time
                // yet it is possible to always cast it to Task, then by casting it to a dynamic object access the result
                if (exception != null || task.IsCompleted)
                {
                    var res = new ExecuteMethodResult();
                    if (exception != null || task.IsFaulted)
                    {
                        res.Data = null;
                        res.ExceptionAdapter =
                            exception ?? new RemoteExceptionAdapter("A remote task failed", task.Exception);
                    }

                    //else the task is completed
                    else if (overload.RealReturnType == typeof(void))
                    {
                        res.ExceptionAdapter = null;
                        res.Data = null;
                    }
                    else
                    {
                        res.Data = (object) ((dynamic) task).Result;
                        res.ExceptionAdapter = null;
                    }

                    req.SendResponse(res);
                    return;
                }

                if (overload.RealReturnType == typeof(void))
                    task.ContinueWith(t =>
                    {
                        RemoteExceptionAdapter ex = null;
                        if (t.Exception != null) ex = new RemoteExceptionAdapter("A remote task failed", t.Exception);
                        req.SendResponse(new ExecuteMethodResult {Data = null, ExceptionAdapter = ex});
                    });
                else
                    task.ContinueWith(t =>
                    {
                        var exe = new ExecuteMethodResult();
                        if (t.Exception != null)
                        {
                            exe.Data = null;
                            exe.ExceptionAdapter = new RemoteExceptionAdapter("A remote task failed", t.Exception);
                        }
                        else
                        {
                            exe.ExceptionAdapter = null;
                            exe.Data = ((dynamic) t).Result;
                        }

                        req.SendResponse(exe);
                    });
            }
        }

        public void SendState(IRequestWrapper request)
        {
            request.SendResponse(Context.All.ContainsClient(request.Connection)
                ? GetAllProperties(request.Connection)
                : new UpdateProperties() {Eid = this.Eid, Updates = null});
        }

        public void AddClient(IConnection client)
        {
            Context?.All.AddClient(client);
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Extensions;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Interfaces;
using Microsoft.CSharp.RuntimeBinder;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledHostedObjectBase : EntangledObjectBase
    {
        private readonly HashSet<Reflection.PropertyDescriptor> _pendingUpdates = new HashSet<Reflection.PropertyDescriptor>();
        protected object _sync = new object();

        public EntangledHostedObjectBase(Guid eid, InterfaceDescriptor i)
        {
            _Eid = eid;
            _Descriptor = i;
            _Context = new EntanglementProviderContext();
            PropertyChanged += EntangledHostedObjectBase_PropertyChanged;
        }

        protected EntanglementProviderContext _Context { get; }

        private void EntangledHostedObjectBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_Descriptor.Properties.TryGetValue(e.PropertyName, out var prop))
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
            var packet = new UpdateProperties {Updates = new List<PropertyData>(), Eid = _Eid};
            using (var ms = MemoryManager.Instance.GetStream())
            {
                foreach (var prop in _Descriptor.Properties)
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
                if (_pendingUpdates.Count == 0 || _Context.All.Clients.Count == 0) return;
                serializer = _Context.All.Clients.First().Serializer;
                packet = new UpdateProperties {Updates = new List<PropertyData>(_pendingUpdates.Count), Eid = _Eid};
                using (var ms = MemoryManager.Instance.GetStream())
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

            _Context.All.Send(packet);
        }


        public void Execute(IRequestWrapper req)
        {
            var cmd = (ExecuteMethod) req.Request;

            //find the best overload
            var overload = _Descriptor.FindOverload(cmd);
            
            lock (_Context)
            {
                if (!_Context.All.ContainsClient(req.Connection))
                {
                    req.SendResponse(new ExecuteMethodResult
                    {
                        ExceptionAdapter = new RemoteExceptionAdapter("Unauthorized")
                    });
                    return;
                }

                _Context.Sender = req.Connection;
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
                        res.SerializedData = null;
                        res.ExceptionAdapter =
                            exception ?? new RemoteExceptionAdapter("A remote task failed", task.Exception);
                    }

                    //else the task is completed
                    else if (overload.RealReturnType == typeof(void))
                    {
                        res.ExceptionAdapter = null;
                        res.SerializedData = null;
                    }
                    else
                    {
                        res.SerializedData = InternalExtensions.Serialize(((dynamic) task).Result, req.Connection.Serializer);
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
                        req.SendResponse(new ExecuteMethodResult {SerializedData = null, ExceptionAdapter = ex});
                    });
                else
                    task.ContinueWith(t =>
                    {
                        var exe = new ExecuteMethodResult();
                        if (t.Exception != null)
                        {
                            exe.SerializedData = null;
                            exe.ExceptionAdapter = new RemoteExceptionAdapter("A remote task failed", t.Exception);
                        }
                        else
                        {
                            exe.ExceptionAdapter = null;
                            exe.SerializedData = InternalExtensions.Serialize(((dynamic)task).Result, req.Connection.Serializer);
                        }

                        req.SendResponse(exe);
                    });
            }
        }

        public void SendState(IRequestWrapper request)
        {
            request.SendResponse(_Context.All.ContainsClient(request.Connection)
                ? GetAllProperties(request.Connection)
                : new UpdateProperties() {Eid = _Eid, Updates = null});
        }

        public void AddClient(IConnection client)
        {
            _Context?.All.AddClient(client);
        }
    }
}
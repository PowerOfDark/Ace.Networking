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
using Ace.Networking.Handlers;
using Ace.Networking.Threading;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Serializers;
using Microsoft.CSharp.RuntimeBinder;
using System.Threading;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledHostedObjectBase : EntangledObjectBase
    {

        public virtual bool ShouldPushUpdates(InternalPropertyData property)
        {
            return true;
        }

        public class InternalPropertyData
        {
            public bool IsPushed { get; set; }
            public PropertyData Data { get; set; }
            public Reflection.PropertyDescriptor Descriptor { get; set; }

            public override int GetHashCode()
            {
                return Data?.GetHashCode() ?? Descriptor?.GetHashCode() ?? 0;
            }

            public override bool Equals(object obj)
            {
                return obj is InternalPropertyData d && d.Data == this.Data;
            }
        }

        private volatile bool _initialized = false;

        private readonly HashSet<InternalPropertyData> _pendingUpdates = new HashSet<InternalPropertyData>();
        protected object _sync = new object();

        private readonly Dictionary<string, InternalPropertyData> _cache =
            new Dictionary<string, InternalPropertyData>();


        private void _initialize()
        {
            lock (_sync)
            {
                if (_initialized) return;
                _initialized = true;
                foreach (var evkv in _Descriptor.Events)
                {
                    if (evkv.Key == "PropertyChanged")
                        continue;
                    var ev = evkv.Value;
                    if (ev.HandlerDelegate == null)
                        _Descriptor.AddEventHandlerDelegate(ev, typeof(EntangledHostedObjectBase));
                    ev.Event.AddEventHandler(this, ev.HandlerDelegate.CreateDelegate(ev.Event.EventHandlerType, this));
                }

                foreach (var ml in _Descriptor.Methods)
                {
                    foreach (var method in ml.Value)
                    {
                        if (method.InvokerDelegate == null)
                            _Descriptor.AddMethodDelegate(method);
                    }
                }
            }

            PropertyChanged += EntangledHostedObjectBase_PropertyChanged;
        }

        public void Attach(ICommon host)
        {
            if (host == null) return;
            if (!_initialized)
                _initialize();
            lock (_sync)
            {
                _Context = new EntanglementProviderContext(host);
                foreach (var prop in _Descriptor.Properties)
                {
                    var e = _cache[prop.Key] = new InternalPropertyData()
                    {
                        Descriptor = prop.Value,
                        IsPushed = false,
                        Data = new PropertyData() { PropertyName = prop.Key, SerializedData = null }
                    };
                    UpdateProperty(e, true);
                }
            }
        }

        public void Detach()
        {
            //TODO:??
            _pendingUpdates.Clear();
            _cache.Clear();
        }

        public EntangledHostedObjectBase(Guid eid, InterfaceDescriptor i, ICommon host)
        {
            _Eid = eid;
            _Descriptor = i;

            Attach(host);
        }

        internal void OnEvent(string name, object[] args)
        {
            for (int i = 0; i < (args?.Length ?? 0); i++)
            {
                if (ReferenceEquals(args[i], this))
                    args[i] = SelfPlaceholder.Instance;
            }
            _Context.All.Send<RaiseEvent>(new RaiseEvent() { Eid = this._Eid, Objects = args, Event = name });
        }

        protected EntanglementProviderContext _Context { get; private set; }

        private void EntangledHostedObjectBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_cache.TryGetValue(e.PropertyName, out var prop))
            {
                lock (_sync)
                {
                    lock (prop)
                    {
                        if (prop.IsPushed)
                        {
                            prop.IsPushed = false;
                            _pendingUpdates.Add(prop);
                        }
                    }
                }
                if (ShouldPushUpdates(prop))
                    PushPropertyUpdates();
            }
        }

        protected UpdateProperties GetAllProperties(IConnection con)
        {
            lock (_sync)
            {
                var packet = new UpdateProperties { Updates = new List<PropertyData>(_Descriptor.Properties.Count), Eid = _Eid };
                foreach (var prop in _cache)
                {
                    packet.Updates.Add(prop.Value.Data);
                }
                return packet;
            }
        }

        protected void UpdateProperty(InternalPropertyData d, bool push = false)
        {
            lock (d)
            {
                d.Data.SerializedData = InternalExtensions.Serialize(d.Descriptor.Property.GetValue(this), this._Context.Host.Serializer);
                d.IsPushed = push;
            }
        }

        protected void PushPropertyUpdates()
        {
            UpdateProperties packet;
            IPayloadSerializer serializer;
            lock (_sync)
            {
                if (_pendingUpdates.Count == 0 || _Context.All.Clients.Count == 0) return;
                serializer = _Context.Host.Serializer;
                packet = new UpdateProperties { Updates = new List<PropertyData>(_pendingUpdates.Count), Eid = _Eid };
                foreach (var prop in _pendingUpdates)
                {
                    if (prop.IsPushed) continue;
                    UpdateProperty(prop, true);
                    packet.Updates.Add(prop.Data);
                }

                _pendingUpdates.Clear();
            }

            _Context.All.Send(packet);
        }


        private SemaphoreSlim _executeLock = new SemaphoreSlim(1);

        public async Task ExecuteAsync(object state)
        {
            await _executeLock.WaitAsync();
            try
            {
                var req = (IRequestWrapper)state;
                var cmd = (ExecuteMethod)req.Request;
                var overload = _Descriptor.FindOverload(cmd);

                ExecuteMethodResult result = null;

                if(overload == null)
                {
                    result = new ExecuteMethodResult()
                    {
                        ExceptionAdapter = new RemoteExceptionAdapter("No overload could be found")
                    };
                }
                
                if (!_Context.All.ContainsClient(req.Connection))
                {
                    result = new ExecuteMethodResult()
                    {
                        ExceptionAdapter = new RemoteExceptionAdapter("Unauthorized")
                    };
                }

                if(result != null)
                {
                    req.TrySendResponse(result, out _);
                    return;
                }

                _Descriptor.FillInvocation(cmd, overload);
                _Context.Sender = req.Connection;
                RemoteExceptionAdapter exception = null;
                Task task = null;
                object retObj = null;
                try
                {
                    retObj = overload.InvokerDelegate.Invoke(this, (cmd.Objects?.Length ?? 0) == 0 ? null : cmd.Objects);
                    if (overload.IsAsync)
                        task = (Task)retObj;
                }
                catch (Exception e)
                {
                    exception = new RemoteExceptionAdapter("A remote task failed", e);
                }

                if (!overload.IsAsync)
                {
                    var m = new ExecuteMethodResult()
                    {
                        ExceptionAdapter = exception,
                        Data = null
                    };
                    if (overload.RealReturnType != typeof(void) && retObj != null)
                    {
                        m.Data = retObj;
                    }

                    req.TrySendResponse(m, out var responseTask);
                    return;
                }


                try
                {
                    if(exception != null)
                        await task;
                }
                catch (Exception e)
                {
                    if (exception == null)
                        exception = new RemoteExceptionAdapter("A remote async task failed", e);
                }

                // HUGE HACK WARNING
                // we need to somehow operate on Task<T>, where T is unknown at compile time
                // yet it is possible to always cast it to Task, then by casting it to a dynamic object access the result

                {
                    var res = new ExecuteMethodResult();
                    if (exception != null)
                    {
                        res.Data = null;
                        res.ExceptionAdapter =
                            exception ?? new RemoteExceptionAdapter("A remote task failed", task.Exception);
                    }

                    //else the task is completed
                    else if (overload.RealReturnType == typeof(void))
                    {
                        res.Data = null;
                    }
                    else
                    {
                        res.Data = ((dynamic)task).Result;
                    }

                    req.TrySendResponse(res, out _);
                    return;
                }
            }
            finally
            {
                _executeLock.Release();
            }

        }

        public void Execute(IRequestWrapper req)
        {
            var cmd = (ExecuteMethod)req.Request;

            //find the best overload
            Task.Factory.StartNew(ExecuteAsync, req, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
        }

        public void SendState(IRequestWrapper request)
        {
            request.TrySendResponse(_Context.All.ContainsClient(request.Connection)
                ? GetAllProperties(request.Connection)
                : new UpdateProperties() { Eid = _Eid, Updates = null }, out _);
        }

        public void AddClient(IConnection client)
        {
            _Context?.All.AddClient(client);
        }
    }
}
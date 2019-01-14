using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Threading;

namespace Ace.Networking.Entanglement
{
    public class EntanglementClientService : IEntanglementClientService
    {
        private IConnection _connection;

        protected ConcurrentDictionary<Guid /*Eid*/, EntangledLocalObjectBase> LocalInstances =
            new ConcurrentDictionary<Guid, EntangledLocalObjectBase>();

        public bool IsActive => _connection != null && _connection.Connected;

        protected HashSet<InterfaceDescriptor> RegisteredTypes = new HashSet<InterfaceDescriptor>();

        public void Attach(IConnection server)
        {
            if (IsActive)
                return;
            LocalInstances.Clear();
            _connection = server;

            RegisterTypes(server);

            server.On<UpdateProperties>(OnUpdateProperties);
            server.On<RaiseEvent>(OnRaiseEvent);
        }

        private void RegisterTypes(IConnection server)
        {
            server.TypeResolver.RegisterAssembly(typeof(EntanglementClientService).GetTypeInfo().Assembly);
        }

        public void Detach(IConnection server)
        {
            if (server == null || !server.Equals(_connection)) return;
            RegisteredTypes.Clear();
            server.Off<UpdateProperties>(OnUpdateProperties);
            server.Off<RaiseEvent>(OnRaiseEvent);
            LocalInstances.Clear();
            _connection = null;
        }

        public async Task<T> Entangle<T>(Guid? eid = null) where T : class/*, IEntangledObject*/
        {
            var typeInfo = typeof(T).GetTypeInfo();
            if (!typeInfo.IsInterface || !typeInfo.IsPublic)
                throw new ArgumentException("The request type must be a public interface");
            var interfaceId = typeInfo.GUID;

            var instance = GetExistingInstance<T>(eid);
            if (instance != null)
                return (T) (object) instance;

            var q = new EntangleRequest {Eid = eid, InterfaceId = interfaceId};
            var result = await _connection.SendRequest<EntangleRequest, EntangleResult>(q).ConfigureAwait(false);
            if (result?.Eid == null) return null;

            instance = GetExistingInstance<T>(result.Eid);
            if (instance != null) return (T) (object) instance;

            instance = EntanglementLocalProxyProvider.Get<T>(_connection, result.Eid.Value);
            RegisterType(instance);
            LocalInstances.TryAdd(result.Eid.Value, instance);

            var props = await _connection.SendRequest<UpdateRequest, UpdateProperties>(new UpdateRequest() {Eid = result.Eid.Value}).ConfigureAwait(false);
            OnUpdateProperties(_connection, props);
            return (T) (object) instance;
        }

        public void RegisterType(EntangledLocalObjectBase obj)
        {
            lock (RegisteredTypes)
            {
                if (!RegisteredTypes.Add(obj._Descriptor)) return;
            }

            var resolver = _connection.TypeResolver;
            var desc = obj._Descriptor;
            foreach (var ml in desc.Methods)
            {
                foreach (var m in ml.Value)
                {
                    resolver.RegisterType(m.RealReturnType);
                    foreach (var p in m.Parameters)
                        resolver.RegisterType(p.Type);
                }
            }

            foreach (var pl in desc.Properties)
            {
                resolver.RegisterType(pl.Value.Property.PropertyType);
            }

            foreach (var ev in desc.Events)
            {
                foreach (var parameter in ev.Value.Parameters)
                {
                    resolver.RegisterType(parameter.Type);
                }
            }
        }

        protected EntangledLocalObjectBase GetExistingInstance<T>(Guid? eid = null) where T : class/*, IEntangledObject*/
        {
            if (eid.HasValue)
                if (LocalInstances.TryGetValue(eid.Value, out var obj))
                    if (obj is T)
                        return obj;
            return null;
        }

        private object OnUpdateProperties(IConnection connection, UpdateProperties payload)
        {
            if (payload != null && LocalInstances.TryGetValue(payload.Eid, out var instance))
                instance.UpdateProperties(connection, payload);
            return null;
        }


        private object OnRaiseEvent(IConnection connection, RaiseEvent payload)
        {
            if (payload != null && LocalInstances.TryGetValue(payload.Eid, out var instance))
                instance.RaiseEvent(connection, payload);
            return null;
        }


    }
}
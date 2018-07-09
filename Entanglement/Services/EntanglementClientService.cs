using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement
{
    public class EntanglementClientService : IEntanglementClientService
    {
        private IConnection _connection;

        protected ConcurrentDictionary<Guid /*Eid*/, EntangledLocalObjectBase> LocalInstances =
            new ConcurrentDictionary<Guid, EntangledLocalObjectBase>();

        public bool IsActive => _connection != null && _connection.Connected;

        public void Attach(IConnection server)
        {
            if (IsActive)
                return;
            LocalInstances.Clear();
            _connection = server;

            server.On<UpdateProperties>(OnUpdateProperties);
        }

        public void Detach(IConnection server)
        {
            if (server == null || !server.Equals(_connection)) return;

            server.Off<UpdateProperties>(OnUpdateProperties);
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
            var result = await _connection.SendRequest<EntangleRequest, EntangleResult>(q);
            if (result?.Eid == null) return null;

            instance = GetExistingInstance<T>(result.Eid);
            if (instance != null) return (T) (object) instance;

            instance = EntanglementLocalProxyProvider.Get<T>(_connection, result.Eid.Value);
            LocalInstances.TryAdd(result.Eid.Value, instance);

            var test = await _connection.SendRequest<UpdateRequest, UpdateProperties>(new UpdateRequest() {Eid = result.Eid.Value});

            return (T) (object) instance;
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
    }
}
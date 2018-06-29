using System;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class EntanglementClient
    {
        public static async Task<T> Entangle<T>(this IConnection connection, Guid? eid = null)
            where T : class, IEntangledObject
        {
            var interfaceId = typeof(T).GetTypeInfo().GUID;
            var instance = getExistingInstance<T>(connection, eid);
            if (instance != null) return instance;

            var q = new EntangleRequest {Eid = eid, InterfaceId = interfaceId};
            var result = await connection.SendRequest<EntangleRequest, EntangleResult>(q);
            if (result?.Eid == null) return null;
            instance = getExistingInstance<T>(connection, result.Eid);
            if (instance != null) return instance;
            instance = EntanglementLocalProxyProvider.Get<T>(connection, result.Eid.Value);
            if (connection.Data != null)
                connection.Data[$"__E{eid.ToString()}"] = instance;
            return instance;
        }

        private static T getExistingInstance<T>(this IConnection connection, Guid? eid)
            where T : class, IEntangledObject
        {
            if (eid.HasValue)
            {
                var existingInstance = connection.Data?.Get<T>($"__E{eid.ToString()}", null);
                if (existingInstance != null)
                    return existingInstance;
            }

            return null;
        }
    }
}
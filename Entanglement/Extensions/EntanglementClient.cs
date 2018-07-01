using System;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class EntanglementClient
    {
        public static Task<T> Entangle<T>(this IConnection connection, Guid? eid = null)
            where T : class, IEntangledObject
        {
            var service = connection.Services.Get<IEntanglementClientService>();
            if (service == null)
                throw new InvalidOperationException(
                    $"The supported connection does not have a {nameof(IEntanglementClientService)} service");
            return service.Entangle<T>(eid);
        }

    }
}
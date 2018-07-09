using System;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement
{
    public static class EntanglementClientExtensions
    {
        public static Task<T> Entangle<T>(this IConnection connection, Guid? eid = null)
            where T : class/*, IEntangledObject*/
        {
            var service = connection.Services.Get<IEntanglementClientService>();
            if (service == null)
                throw new InvalidOperationException(
                    $"The supported connection does not have a {nameof(IEntanglementClientService)} service");
            return service.Entangle<T>(eid);
        }
    }
}
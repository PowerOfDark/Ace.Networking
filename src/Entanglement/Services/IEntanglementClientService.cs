using System;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Services
{
    public interface IEntanglementClientService : IService<IConnection>
    {
        Task<T> Entangle<T>(Guid? eid = null) where T : class/*, IEntangledObject*/;
    }
}
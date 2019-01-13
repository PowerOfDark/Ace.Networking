using System.Collections.Generic;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Structures
{
    public interface IConnectionGroup : IMulticastDispatcherInterface
    {
        ICommon Host { get; }
        IReadOnlyCollection<IConnection> Clients { get; }
        void AddClient(IConnection client);
        bool RemoveClient(IConnection client);
        bool ContainsClient(IConnection client);
    }
}
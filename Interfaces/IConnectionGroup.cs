using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionGroup : IMulticastConnectionInterface
    {
        IEnumerable<IConnection> Clients { get; }
        void AddClient(IConnection client);
        bool RemoveClient(IConnection client);
        bool ContainsClient(IConnection client);
    }
}

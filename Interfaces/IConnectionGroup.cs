using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionGroup : IConnectionInterface
    {
        IEnumerable<IConnection> Clients { get; }
        void AddClient(IConnection client);
        void RemoveClient(IConnection client);
        bool ContainsClient(IConnection client);
    }
}

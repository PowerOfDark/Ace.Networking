﻿using System.Collections.Generic;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionGroup : IMulticastDispatcherInterface
    {
        IReadOnlyCollection<IConnection> Clients { get; }
        void AddClient(IConnection client);
        bool RemoveClient(IConnection client);
        bool ContainsClient(IConnection client);
    }
}
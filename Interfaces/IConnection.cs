using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Interfaces
{
    public interface IConnection : IConnectionInterface
    {
        long Identifier { get; }
        Guid Guid { get; }
        IPayloadSerializer Serializer {get;}
        IConnectionData Data { get; }
        bool Connected { get; }
        DateTime LastReceived { get; }
    }
}

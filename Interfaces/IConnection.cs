using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Interfaces
{
    public interface IConnection : IConnectionInterface
    {
        Guid Guid { get; }
        IPayloadSerializer Serializer {get;}
        bool Connected { get; }
    }
}

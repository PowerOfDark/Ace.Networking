using System;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Interfaces
{
    public interface IConnection : IConnectionInterface, IServiceContainer<IConnection>
    {
        long Identifier { get; }
        Guid Guid { get; }
        IPayloadSerializer Serializer { get; }
        IConnectionData Data { get; }
        bool Connected { get; }
        DateTime LastReceived { get; }

        void Initialize();
    }
}
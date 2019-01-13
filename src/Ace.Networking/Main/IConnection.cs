using System;
using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Threading
{
    public interface IConnection : IConnectionInterface, IServiceContainer<IConnection>
    {
        long Identifier { get; }
        Guid Guid { get; }
        IConnectionData Data { get; }
        bool Connected { get; }
        DateTime LastReceived { get; }

        void Initialize();

        event GlobalPayloadHandler PayloadSent;
        event RawDataHeader.RawDataHandler RawDataReceived;


        int CreateNewRawDataBuffer();
        bool DestroyRawDataBuffer(int bufId);
        void OnRaw(int bufId, RawDataHeader.RawDataHandler handler);
        bool OffRaw(int bufId, RawDataHeader.RawDataHandler handler);
    }
}
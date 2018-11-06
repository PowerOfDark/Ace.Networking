using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Interfaces
{
    public interface ICommon : IConnectionDispatcherInterface, INotifyClientDisconnected
    {
        event GlobalPayloadHandler PayloadReceived;
        IPayloadSerializer Serializer { get; }
        ITypeResolver TypeResolver { get; }
        event Connection.InternalPayloadDispatchHandler DispatchPayload;
    }
}
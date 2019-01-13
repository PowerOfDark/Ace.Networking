using Ace.Networking.Handlers;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Serializers;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public interface ICommon : IConnectionDispatcherInterface, INotifyClientDisconnected
    {
        event GlobalPayloadHandler PayloadReceived;
        IPayloadSerializer Serializer { get; }
        ITypeResolver TypeResolver { get; }
        event Connection.InternalPayloadDispatchHandler DispatchPayload;
    }
}
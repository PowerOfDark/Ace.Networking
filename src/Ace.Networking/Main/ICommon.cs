using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking.Threading
{
    public interface ICommon : IConnectionDispatcherInterface, INotifyClientDisconnected
    {
        event GlobalPayloadHandler PayloadReceived;
        IPayloadSerializer Serializer { get; }
        ITypeResolver TypeResolver { get; }
        event Connection.InternalPayloadDispatchHandler DispatchPayload;
    }
}
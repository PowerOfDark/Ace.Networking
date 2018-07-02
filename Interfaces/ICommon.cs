using Ace.Networking.Handlers;

namespace Ace.Networking.Interfaces
{
    public interface ICommon : IConnectionDispatcherInterface, INotifyClientDisconnected
    {
        event GlobalPayloadHandler PayloadReceived;
    }
}
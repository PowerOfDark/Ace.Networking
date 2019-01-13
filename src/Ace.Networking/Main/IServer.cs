using Ace.Networking.Services;

namespace Ace.Networking
{
    public interface IServer : ICommon, IServiceContainer<IServer>
    {
        event TcpServer.ClientAcceptedHandler ClientAccepted;
        //event Connection.InternalPayloadDispatchHandler DispatchPayload;
    }
}
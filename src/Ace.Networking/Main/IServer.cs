using Ace.Networking.Services;

namespace Ace.Networking
{
    public interface IServer : ICommon
    {
        event TcpServer.ClientAcceptedHandler ClientAccepted;
        //event Connection.InternalPayloadDispatchHandler DispatchPayload;
    }
}
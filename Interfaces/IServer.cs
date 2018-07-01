using Ace.Networking.Services;

namespace Ace.Networking.Interfaces
{
    public interface IServer : ICommon, IServiceContainer<IServer>
    {
        event TcpServer.ClientAcceptedHandler ClientAccepted;
    }
}
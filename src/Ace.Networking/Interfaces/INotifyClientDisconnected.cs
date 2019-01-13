using Ace.Networking;

namespace Ace.Networking.Interfaces
{
    public interface INotifyClientDisconnected
    {
        event Connection.DisconnectHandler ClientDisconnected;
    }
}
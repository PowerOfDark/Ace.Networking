namespace Ace.Networking.Threading
{
    public interface INotifyClientDisconnected
    {
        event Connection.DisconnectHandler ClientDisconnected;
    }
}
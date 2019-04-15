using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IServiceManager
    {
        T Get<T>() where T : class;
    }
}
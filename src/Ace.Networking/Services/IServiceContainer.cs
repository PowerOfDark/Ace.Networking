using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IServiceContainer
    {
        IServiceManager Services { get; }
    }
}
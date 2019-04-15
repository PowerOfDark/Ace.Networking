using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IInternalServiceManager<T> : IServiceManager, IAttachable<T> where T : class, ICommon
    {
    }
}
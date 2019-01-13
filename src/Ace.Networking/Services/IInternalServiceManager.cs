using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IInternalServiceManager<T> : IServiceManager<T>, IAttachable<T> where T : class, ICommon
    {
    }
}
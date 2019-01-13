using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IInternalServiceManager<T> : IServiceManager<T>, IAttachable<T> where T : class, ICommon
    {
    }
}
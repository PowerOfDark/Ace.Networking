using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IServiceContainer<TInterface> where TInterface : class, ICommon
    {
        IServiceManager<TInterface> Services { get; }
    }
}
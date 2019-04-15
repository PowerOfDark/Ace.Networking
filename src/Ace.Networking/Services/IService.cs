using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IServiceBase
    {
        bool IsActive { get; }
    }
    public interface IService<T> : IServiceBase where T: class, ICommon
    {
        void Attach(T server);
        void Detach(T server);
    }
}
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IService<in T> where T : class, ICommon
    {
        bool IsActive { get; }

        void Attach(T server);
        void Detach(T server);
    }
}
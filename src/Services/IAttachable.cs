using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IAttachable<in TInterface> where TInterface : class, ICommon
    {
        void Attach(TInterface connection);
        void Detach(TInterface connection);
    }
}
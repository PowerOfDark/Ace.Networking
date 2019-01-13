using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IAttachable<in TInterface> where TInterface : class, ICommon
    {
        void Attach(TInterface connection);
        void Detach(TInterface connection);
    }
}
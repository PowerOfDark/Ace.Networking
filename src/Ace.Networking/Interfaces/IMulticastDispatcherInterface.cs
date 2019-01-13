using System.Threading.Tasks;

namespace Ace.Networking.Threading
{
    public interface IMulticastDispatcherInterface : ICommon
    {
        void Close();
        Task Send<T>(T data);
    }
}
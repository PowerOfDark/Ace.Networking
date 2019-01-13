using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IMulticastDispatcherInterface : ICommon
    {
        void Close();
        Task Send<T>(T data);
    }
}
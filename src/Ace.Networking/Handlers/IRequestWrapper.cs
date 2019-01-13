using System.Threading.Tasks;

namespace Ace.Networking.Threading
{
    public interface IRequestWrapper
    {
        object Request { get; }
        IConnection Connection { get; }
        Task SendResponse<T>(T response);
    }
}
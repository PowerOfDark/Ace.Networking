using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IRequestWrapper
    {
        object Request { get; }
        IConnection Connection { get; }
        Task SendResponse<T>(T response);
    }
}
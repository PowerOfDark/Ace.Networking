using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionInterface : IMulticastDispatcherInterface
    {
        Task<TResponse> SendRequest<TSend, TResponse>(TSend data, CancellationToken? token = null);

        Task<object> SendRequest<TRequest>(TRequest req, CancellationToken? token = null);

        Task<TReceive> SendReceive<TSend, TReceive>(TSend obj, CancellationToken? token = null);

        Task<object> SendReceive<TSend>(TSend obj, Connection.PayloadFilter filter,
            CancellationToken? token = null);

        Task EnqueueSendResponse<T>(int requestId, T response);
    }
}
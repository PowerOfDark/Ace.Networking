using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionInterface
    {
        Task Send<T>(T data);
        Task<TResponse> SendRequest<TSend, TResponse>(TSend data, CancellationToken? token = null);
        Task EnqueueSendResponse<T>(int requestId, T response);

        void Close();
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionInterface : IMulticastConnectionInterface
    {
        
        Task<TResponse> SendRequest<TSend, TResponse>(TSend data, CancellationToken? token = null);
        Task EnqueueSendResponse<T>(int requestId, T response);

    }
}

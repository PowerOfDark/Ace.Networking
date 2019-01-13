using System.Threading.Tasks;
using Ace.Networking.Threading;

namespace Ace.Networking.Handlers
{
    public class RequestWrapper : IRequestWrapper
    {
        internal RequestWrapper(IConnection connection, int id, object request)
        {
            Connection = connection;
            RequestId = id;
            Request = request;
        }

        internal int RequestId { get; }

        public object Request { get; }
        public IConnection Connection { get; }

        public Task SendResponse<T>(T response)
        {
            return Connection.EnqueueSendResponse(RequestId, response);
        }

        public bool TrySendResponse<T>(T response, out Task task)
        {
            if (!Connection.Connected)
            {
                task = null;
                return false;
            }

            task = Connection.EnqueueSendResponse(RequestId, response);
            return true;
        }
    }
}
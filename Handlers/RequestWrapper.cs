using System.Threading.Tasks;

namespace Ace.Networking.Handlers
{
    public class RequestWrapper
    {
        internal RequestWrapper(Connection connection, int id, object request)
        {
            Connection = connection;
            RequestId = id;
            Request = request;
        }

        public object Request { get; }
        public Connection Connection { get; }
        internal int RequestId { get; }

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
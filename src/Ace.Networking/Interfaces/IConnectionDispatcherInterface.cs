using System;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionDispatcherInterface
    {
        void On<T>(GenericPayloadHandler<T> handler);
        void On(Type type, PayloadHandler handler);
        void On<T>(PayloadHandler handler);

        bool Off<T>(GenericPayloadHandler<T> handler);
        bool Off(Type type, PayloadHandler handler);
        bool Off<T>(PayloadHandler handler);
        bool Off(Type type);
        bool Off<T>();
        void Off();


        void OnRequest<T>(RequestHandler handler);

        void OnRequest(Type type, RequestHandler handler);
        //IReadOnlyCollection<RequestHandler> OnRequest(Type type);
        //IReadOnlyCollection<RequestHandler> OnRequest<T>();

        bool OffRequest<T>(RequestHandler handler);
        bool OffRequest(Type type);
        bool OffRequest(Type type, RequestHandler handler);
        bool OffRequest<T>();

        Task<object> Receive(Connection.PayloadFilter filter, CancellationToken? token = null);
        Task<object> Receive(Type type, CancellationToken? token = null);
        Task<T> Receive<T>(CancellationToken? token = null);

        Task<IRequestWrapper> ReceiveRequest(Type type, CancellationToken? token = null);
        Task<IRequestWrapper> ReceiveRequest<T>(CancellationToken? token = null);
    }
}
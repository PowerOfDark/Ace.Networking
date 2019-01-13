using System;
using System.Collections.Generic;
using Ace.Networking.Handlers;

namespace Ace.Networking.Threading
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


        void OnRequest<T>(RequestHandler handler);
        void OnRequest(Type type, RequestHandler handler);
        //IReadOnlyCollection<RequestHandler> OnRequest(Type type);
        //IReadOnlyCollection<RequestHandler> OnRequest<T>();

        bool OffRequest<T>(RequestHandler handler);
        bool OffRequest(Type type);
        bool OffRequest(Type type, RequestHandler handler);
        bool OffRequest<T>();

    }
}
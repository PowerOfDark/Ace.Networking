using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Ace.Networking.Handlers;

namespace Ace.Networking.Interfaces
{
    public interface IServer
    {
        
        event TcpServer.ClientAcceptedHandler ClientAccepted;
        event Connection.DisconnectHandler ClientDisconnected;

        void On<T>(PayloadHandlerDispatcherBase.PayloadHandler handler);
        void On<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler);

        bool Off<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler);
        bool Off<T>(PayloadHandlerDispatcherBase.PayloadHandler handler);

        void OnRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler);
        bool OffRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler);
    }
}

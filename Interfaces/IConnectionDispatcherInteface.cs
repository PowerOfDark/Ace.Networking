using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Handlers;

namespace Ace.Networking.Interfaces
{
    public interface IConnectionDispatcherInteface
    {
        void OnRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler);
        bool OffRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler);
        void On<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler);
        bool Off<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler);
    }
}

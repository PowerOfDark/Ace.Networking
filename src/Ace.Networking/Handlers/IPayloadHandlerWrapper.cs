using System;
using Ace.Networking.Threading;

namespace Ace.Networking.Handlers
{
    public interface IPayloadHandlerWrapper
    {
        object Invoke(IConnection connection, object obj, Type type);

        bool HandlerEquals(object obj);
    }
}
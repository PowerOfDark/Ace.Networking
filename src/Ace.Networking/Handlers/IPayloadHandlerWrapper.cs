using System;
using Ace.Networking.Threading;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadHandlerWrapper
    {
        object Invoke(IConnection connection, object obj, Type type);

        bool HandlerEquals(object obj);
    }
}
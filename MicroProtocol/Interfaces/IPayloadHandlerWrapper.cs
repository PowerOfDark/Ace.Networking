using System;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadHandlerWrapper
    {
        object Invoke(Connection connection, object obj, Type type);

        bool HandlerEquals(object obj);
    }
}
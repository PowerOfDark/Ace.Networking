using System;
using Ace.Networking.Interfaces;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadHandlerWrapper
    {
        object Invoke(IConnection connection, object obj, Type type);

        bool HandlerEquals(object obj);
    }
}
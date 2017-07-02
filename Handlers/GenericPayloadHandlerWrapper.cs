using System;
using System.Runtime.CompilerServices;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Handlers
{
    public class GenericPayloadHandlerWrapper<T> : IPayloadHandlerWrapper
    {
        public GenericPayloadHandlerWrapper(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler)
        {
            Handler = handler;
        }

        public PayloadHandlerDispatcherBase.GenericPayloadHandler<T> Handler { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Invoke(Connection connection, object obj, Type type)
        {
            return Handler.Invoke(connection, (T) obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HandlerEquals(object obj)
        {
            return Handler.Equals(obj);
        }
    }
}
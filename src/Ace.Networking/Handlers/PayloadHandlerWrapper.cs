using System;
using System.Runtime.CompilerServices;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Threading;

namespace Ace.Networking.Handlers
{
    public class PayloadHandlerWrapper : IPayloadHandlerWrapper
    {
        public PayloadHandlerWrapper(PayloadHandler handler)
        {
            Handler = handler;
        }

        public PayloadHandler Handler { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Invoke(IConnection connection, object obj, Type type)
        {
            return Handler.Invoke(connection, obj, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HandlerEquals(object obj)
        {
            return Handler.Equals(obj);
        }
    }
}
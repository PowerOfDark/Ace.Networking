using System;
using System.Runtime.CompilerServices;

namespace Ace.Networking.Handlers
{
    public abstract class PayloadHandlerDispatcher : PayloadHandlerDispatcherBase
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendPayloadHandler(Type type, PayloadHandler handler)
        {
            AppendTypeHandler(type, new PayloadHandlerWrapper(handler));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendPayloadHandler<T>(PayloadHandler handler)
        {
            AppendPayloadHandler(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemovePayloadHandler(Type type, PayloadHandler handler)
        {
            return RemoveTypeHandler(type, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemovePayloadHandler<T>(PayloadHandler handler)
        {
            return RemovePayloadHandler(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendGenericPayloadHandler<T>(GenericPayloadHandler<T> handler)
        {
            AppendTypeHandler(typeof(T), new GenericPayloadHandlerWrapper<T>(handler));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveGenericPayloadHandler<T>(GenericPayloadHandler<T> handler)
        {
            return RemoveTypeHandler(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void On<T>(GenericPayloadHandler<T> handler)
        {
            AppendGenericPayloadHandler(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void On<T>(PayloadHandler handler)
        {
            AppendPayloadHandler<T>(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off<T>(GenericPayloadHandler<T> handler)
        {
            return RemoveGenericPayloadHandler(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off<T>(PayloadHandler handler)
        {
            return RemovePayloadHandler<T>(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off<T>()
        {
            return RemoveAllTypeHandlers(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Off()
        {
            RemoveAllTypeHandlers();
        }

        protected void ProcessPayloadHandlers(Connection connection, object obj, Type type,
            Action<object> responseSender = null)
        {
            if (TypeHandlers.Count > 0)
            {
                if (TypeHandlers.ContainsKey(type))
                {
                    lock (TypeHandlers[type])
                    {
                        try
                        {
                            foreach (var f in TypeHandlers[type])
                            {
                                var r = f.Invoke(connection, obj, type);
                                if (r != null)
                                {
                                    responseSender?.Invoke(r);
                                }
                            }
                        }
                        catch
                        {
                        }
                        //TODO: Inconsistencies
                        // An exception in one of the handlers breaks the chain
                    }
                }
            }
        }
    }
}
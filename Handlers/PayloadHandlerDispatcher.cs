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

        /// <summary>
        /// WARNING: This function overwrites the specified request handler
        /// </summary>
        public void OnRequest(Type type, RequestHandler handler)
        {
            if (!RequestHandlers.TryAdd(type, handler))
            {
                try
                {
                    RequestHandlers[type] = handler;
                }
                catch { }
            }
        }

        /// <summary>
        /// WARNING: This function overwrites the specified request handler
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRequest<T>(RequestHandler handler)
        {
            OnRequest(typeof(T), handler);
        }

        /// <summary>
        /// Returns the current request handler for the specified type, or null if doesn't exist
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RequestHandler OnRequest(Type type)
        {
            if (RequestHandlers.TryGetValue(type, out var handler))
                return handler;
            return null;
        }

        /// <summary>
        /// Returns the current request handler for the specified type, or null if doesn't exist
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RequestHandler OnRequest<T>()
        {
            return OnRequest(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest(Type type)
        {
            return RequestHandlers.TryRemove(type, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest<T>()
        {
            return OffRequest(typeof(T));
        }

        protected void ProcessPayloadHandlers(Connection connection, object obj, Type type,
            Action<object> responseSender = null, int? requestId = null)
        {
            if (TypeHandlers.TryGetValue(type, out var list))
            {
                lock (list)
                {
                    try
                    {
                        foreach (var f in list)
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
                        // ignored
                    }
                    //TODO: Inconsistencies
                    // An exception in one of the handlers breaks the chain
                }
            }

            if (requestId.HasValue)
            {
                if (RequestHandlers.TryGetValue(type, out var handler))
                {
                    try
                    {
                        handler?.Invoke(new RequestWrapper(connection, requestId.Value, obj));
                    }
                    catch { }
                }
            }

        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Helpers;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Handlers
{
    public abstract class PayloadHandlerDispatcher : PayloadHandlerDispatcherBase, IConnectionDispatcherInterface
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRequest<T>(RequestHandler handler)
        {
            OnRequest(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest<T>(RequestHandler handler)
        {
            return OffRequest(typeof(T), handler);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void On<T>(GenericPayloadHandler<T> handler)
        {
            AppendGenericPayloadHandler(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void On(Type type, PayloadHandler handler)
        {
            AppendPayloadHandler(type, handler);
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
        public bool Off(Type type, PayloadHandler handler)
        {
            return RemovePayloadHandler(type, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off<T>(PayloadHandler handler)
        {
            return RemovePayloadHandler<T>(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off(Type type)
        {
            return RemoveAllTypeHandlers(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Off<T>()
        {
            return RemoveAllTypeHandlers(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnRequest(Type type, RequestHandler handler)
        {
            AppendRequestHandler(type, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest(Type type)
        {
            return RemoveAllRequestHandlers(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest(Type type, RequestHandler handler)
        {
            return RemoveRequestHandler(type, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OffRequest<T>()
        {
            return OffRequest(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Off()
        {
            RemoveAllHandlers();
        }

        /// <summary>
        ///     Returns the first packet for which <b>filter</b> yields true
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<object> Receive(Connection.PayloadFilter filter, CancellationToken? token = null)
        {
            var tcs = TaskHelper.New<object>(filter);

            token?.Register(t => ((TaskCompletionSource<object>) t).TrySetCanceled(), tcs);
            AppendFilter(tcs);

            return tcs.Task;
        }


        public Task<object> Receive(Type type, CancellationToken? token = null)
        {
            var tcs = TaskHelper.New<object>();
            AppendReceiveTask(type, tcs);
            token?.Register(t => ((TaskCompletionSource<object>) t).TrySetCanceled(), tcs);
            return tcs.Task;
        }

        public async Task<T> Receive<T>(CancellationToken? token = null)
        {
            var task = Receive(typeof(T), token);
            return (T) await task.ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AppendPayloadHandler(Type type, PayloadHandler handler)
        {
            AppendTypeHandler(type, new PayloadHandlerWrapper(handler));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AppendPayloadHandler<T>(PayloadHandler handler)
        {
            AppendPayloadHandler(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool RemovePayloadHandler(Type type, PayloadHandler handler)
        {
            return RemoveTypeHandler(type, handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool RemovePayloadHandler<T>(PayloadHandler handler)
        {
            return RemovePayloadHandler(typeof(T), handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AppendGenericPayloadHandler<T>(GenericPayloadHandler<T> handler)
        {
            AppendTypeHandler(typeof(T), new GenericPayloadHandlerWrapper<T>(handler));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool RemoveGenericPayloadHandler<T>(GenericPayloadHandler<T> handler)
        {
            return RemoveTypeHandler(typeof(T), handler);
        }

        /// <summary>
        ///     Returns the current request handlers for the specified type, or null if doesn't exist
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyCollection<RequestHandler> OnRequest(Type type)
        {
            if (Bindings.TryGetValue(type, out var binding))
                return binding.RequestHandlers;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyCollection<RequestHandler> OnRequest<T>()
        {
            return OnRequest(typeof(T));
        }

        public Task<IRequestWrapper> ReceiveRequest(Type type, CancellationToken? token = null)
        {
            var tcs = TaskHelper.New<IRequestWrapper>();
            AppendRequestTask(type, tcs);
            token?.Register(t => ((TaskCompletionSource<IRequestWrapper>)t).TrySetCanceled(), tcs);
            return tcs.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IRequestWrapper> ReceiveRequest<T>(CancellationToken? token = null)
        {
            return ReceiveRequest(typeof(T), token);
        }

        protected bool ProcessPayloadHandlers(IConnection connection, object obj, Type type,
            Action<object> responseSender = null, int? requestId = null)
        {
            var handled = false;
            if (Bindings.TryGetValue(type, out var binding))
            {
                if (requestId.HasValue)
                {
                    var wrapper = new RequestWrapper(connection, requestId.Value, obj);
                    lock (binding.RequestHandlers)
                    {
                        foreach (var h in binding.RequestHandlers)
                            try
                            {
                                if (h?.Invoke(wrapper) ?? false)
                                {
                                    handled = true;
                                    break;
                                }
                            }
                            catch
                            {
                            }
                    }

                    if (!handled)
                        lock (binding.RequestTasks)
                        {
                            while (binding.RequestTasks.Count > 0)
                                handled |= binding.RequestTasks.Dequeue().TrySetResult(wrapper);
                        }
                }

                lock (binding.TypeHandlers)
                {
                    try
                    {
                        foreach (var f in binding.TypeHandlers)
                        {
                            var r = f.Invoke(connection, obj, type);
                            responseSender?.Invoke(r);
                            handled |= r != null;
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    //TODO: Inconsistencies
                    // An exception in one of the handlers breaks the chain
                }

                lock (binding.ReceiveTasks)
                {
                    while (binding.ReceiveTasks.Count > 0) handled |= binding.ReceiveTasks.Dequeue().TrySetResult(obj);
                }
            }

            if (ReceiveFilters.Count > 0)
                lock (ReceiveFilters)
                {
                    var enumerator = ReceiveFilters.First;
                    while (enumerator != null)
                    {
                        var delete = true;
                        if (enumerator.Value.Task.AsyncState is Connection.PayloadFilter f)
                            try
                            {
                                if (f(obj, type))
                                    handled |= enumerator.Value.TrySetResult(obj);
                                else
                                    delete = false;
                            }
                            catch
                            {
                                // ignored
                            }

                        var next = enumerator.Next;
                        if (delete) ReceiveFilters.Remove(enumerator);
                        enumerator = next;
                    }
                }

            return handled;
        }
    }
}
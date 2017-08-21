using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.PacketTypes;

namespace Ace.Networking
{
    public static class ConnectionExtensions
    {
        /// <summary>
        ///     Returns the first packet for which <b>filter</b> yields true
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static Task<object> Receive(this Connection connection, Connection.PayloadFilter filter, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<object>(filter);

            lock (connection._receiveFiltersLock)
            {
                connection._receiveFilters.AddLast(tcs);
            }

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t => ((TaskCompletionSource<object>) t).TrySetCanceled(), tcs);
            }
            return tcs.Task;
        }

        public static async Task<T> Receive<T>(this Connection connection, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<object>();
            if (!connection._receiveTypeFilters.TryAddLast(typeof(T), tcs))
            {
                throw new InvalidOperationException();
            }

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t => ((TaskCompletionSource<object>) t).TrySetCanceled(), tcs);
            }
            return (T) await tcs.Task;
        }

        public static Task<object> SendReceive<TSend>(this Connection connection, TSend obj, Connection.PayloadFilter filter,
            TimeSpan? timeout = null)
        {
            var res = Receive(connection, filter, timeout);
            Send(connection, obj);
            return res;
        }

        public static Task<TReceive> SendReceive<TSend, TReceive>(this Connection connection, TSend obj, TimeSpan? timeout = null)
        {
            var res = Receive<TReceive>(connection, timeout);
            Send(connection, obj);
            return res;
        }

        public static Task<object> SendRequest<TRequest>(this Connection connection, TRequest req, TimeSpan? timeout = null)
        {
            var id = Interlocked.Increment(ref Connection._lastRequestId);
            var tcs = new TaskCompletionSource<object>(id);

            if (!connection._responseHandlers.TryAdd(id, tcs))
            {
                throw new InvalidOperationException();
            }

            connection.EnqueueSendPacket(new TrackablePacket<TRequest>(new TrackableHeader(id, PacketFlag.IsRequest), req));

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t =>
                {
                    var task = (TaskCompletionSource<object>) t;
                    task.TrySetCanceled();
                    connection._responseHandlers.TryRemove((int) task.Task.AsyncState, out _);
                }, tcs);
            }
            return tcs.Task;
        }

        public static Task<RequestWrapper> ReceiveRequest(this Connection connection, Type type, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<RequestWrapper>();

            if (!connection._requestHandlers.TryEnqueue(type, tcs))
            {
                throw new InvalidOperationException();
            }

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t => ((TaskCompletionSource<RequestWrapper>) t).TrySetCanceled(), tcs);
            }
            return tcs.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<RequestWrapper> ReceiveRequest<T>(this Connection connection, TimeSpan? timeout = null)
        {
            return ReceiveRequest(connection, typeof(T), timeout);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns>Return value of non-generic SendRequest casted to TResponse</returns>
        public static async Task<TResponse> SendRequest<TRequest, TResponse>(this Connection connection, TRequest req,
            TimeSpan? timeout = null)
        {
            var res = await SendRequest(connection, req, timeout);
            return (TResponse) res;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Send<T>(this Connection connection, T payload)
        {
            return connection.EnqueueSendContent(payload);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task EnqueueSendRaw(this Connection connection, int bufId, int seq, byte[] buf, int count = -1)
        {
            return connection.EnqueueSendPacket(new RawDataPacket(new RawDataHeader(bufId, seq), buf, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task EnqueueSendRaw(this Connection connection, int bufId, byte[] buf, int count = -1)
        {
            return EnqueueSendRaw(connection, bufId, 0, buf, count);
        }

        /// <summary>
        ///     WARNING: This function uses the specified Stream to stream data.
        ///     If <paramref name="disposeAfterSend" /> is true, the Stream will be disposed after the send operation is completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task EnqueueSendRaw(this Connection connection, int bufferId, int seq, Stream stream, int count = -1,
            bool disposeAfterSend = true)
        {
            return connection.EnqueueSendPacket(new RawDataPacket(new RawDataHeader(bufferId, seq, count, disposeAfterSend), stream));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task EnqueueSendRaw(this Connection connection, int bufferId, Stream stream, int count = -1,
            bool disposeAfterSend = true)
        {
            return EnqueueSendRaw(connection, bufferId, 0, stream, count, disposeAfterSend);
        }
    }
}
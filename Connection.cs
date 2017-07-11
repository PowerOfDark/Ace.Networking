#define ReadAsync_Test
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.MicroProtocol.Structures;
using Ace.Networking.Structures;
using Ace.Networking.Threading;
using static Ace.Networking.MicroProtocol.Headers.RawDataHeader;

namespace Ace.Networking
{
    public sealed class Connection : PayloadHandlerDispatcher, IDisposable
    {
        public delegate void DisconnectHandler(Connection connection, Exception exception);

        public delegate bool PayloadFilter(object payload, Type type);

        public const int BufferSize = 16384;

        private static long _lastId = 1;
        private static int _lastRawDataBufferId = 1;
        private static int _lastRequestId = 1;
        private readonly SemaphoreSlim _closeEvent = new SemaphoreSlim(0, 1);

        private readonly IPayloadDecoder _decoder;


        private readonly IPayloadEncoder _encoder;

        private readonly ConcurrentDictionary<int, LinkedList<RawDataHandler>> _rawDataHandlers =
            new ConcurrentDictionary<int, LinkedList<RawDataHandler>>();

        private readonly LinkedList<TaskCompletionSource<object>> _receiveFilters =
            new LinkedList<TaskCompletionSource<object>>();

        private readonly object _receiveFiltersLock = new object();
        private readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);

        /// <summary>
        ///     where object is TaskCompletionSource of T; can't avoid boxing/unboxing
        /// </summary>
        private readonly ConcurrentDictionary<Type, LinkedList<TaskCompletionSource<object>>> _receiveTypeFilters =
            new ConcurrentDictionary<Type, LinkedList<TaskCompletionSource<object>>>();

        private readonly ConcurrentDictionary<Type, Queue<TaskCompletionSource<RequestWrapper>>>
            _requestHandlers =
                new ConcurrentDictionary<Type, Queue<TaskCompletionSource<RequestWrapper>>>();

        private readonly ConcurrentDictionary<int, TaskCompletionSource<object>> _responseHandlers =
            new ConcurrentDictionary<int, TaskCompletionSource<object>>();

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentQueue<TaskCompletionSource<object>> _sendQueue =
            new ConcurrentQueue<TaskCompletionSource<object>>();

        private readonly AutoResetEvent _sendWorkerWaitHandle = new AutoResetEvent(false);
        private readonly ISslStreamFactory _sslFactory;

        private object _payloadPending;
        private Type _payloadPendingType;

        private SocketBuffer _readBuffer;
        private Thread _receiveWorkerThread;
        private volatile bool _receiveWorkerThreadRunning;

        private TaskCompletionSource<object> _sendCompletionSource;
        private Thread _sendWorkerThread;
        private volatile bool _sendWorkerThreadRunning;
        private SslStream _sslStream;

        private Stream _stream;
        private SocketBuffer _writeBuffer;

        internal InternalPayloadDispatchHandler PayloadDispatchHandler;

        private Connection(IPayloadEncoder encoder, IPayloadDecoder decoder)
        {
            Identifier = Interlocked.Increment(ref _lastId);
            Guid = Guid.NewGuid();

            _encoder = encoder;
            _decoder = decoder;
            _readBuffer = new SocketBuffer(BufferSize);
            _writeBuffer = new SocketBuffer(BufferSize);
            _decoder.PacketReceived = OnPayloadReceived;
            _decoder.RawDataReceived = OnRawDataReceived;

            Data = new ConnectionData();
        }

        private Connection(TcpClient client, IPayloadEncoder encoder, IPayloadDecoder decoder) : this(encoder, decoder)
        {
            Client = client;
        }

        public Connection(TcpClient client, ProtocolConfiguration configuration) : this(client,
            configuration.PayloadEncoder.Clone(), configuration.PayloadDecoder.Clone())
        {
            SslMode = configuration.SslMode;
            CustomOutcomingMessageQueue = configuration.CustomOutcomingMessageQueue;
            CustomIncomingMessageQueue = configuration.CustomIncomingMessageQueue;
        }

        public Connection(TcpClient client, ProtocolConfiguration configuration,
            ISslStreamFactory sslFactory) : this(client, configuration)
        {
            _sslFactory = sslFactory;
        }

        public bool UseCustomOutcomingMessageQueue => CustomOutcomingMessageQueue != null;
        public bool UseCustomIncomingMessageQueue => CustomIncomingMessageQueue != null;

        public ThreadedQueueProcessor<ReceiveMessageQueueItem> CustomIncomingMessageQueue { get; }

        public ThreadedQueueProcessor<SendMessageQueueItem> CustomOutcomingMessageQueue { get; }

        public long Identifier { get; }
        public Guid Guid { get; }
        public TcpClient Client { get; private set; }
        public bool Connected { get; private set; }
        public Socket Socket => Client?.Client;
        public Stream Stream => _sslStream ?? _stream;
        public SslMode SslMode { get; }

        public int MessagesQueued
        {
            get
            {
                if (UseCustomOutcomingMessageQueue)
                {
                    throw new NotSupportedException("Connection-binding in custom queue is not supported");
                }
                return _sendQueue.Count;
            }
        }

        public IConnectionData Data { get; set; }
        public ISslCertificatePair SslCertificates => _sslFactory;

        /// <summary>
        ///     Gets the <code>DateTime</code> of the last received packet.
        /// </summary>
        public DateTime LastReceived { get; internal set; }

        public event DisconnectHandler Disconnected;

        /// <summary>
        ///     Catch-all for all payload received.
        ///     This event should be used in a receive-only fashion.
        /// </summary>
        public event GlobalPayloadHandler PayloadReceived;

        public event PayloadHandler PayloadSent;
        public event RawDataHandler RawDataReceived;

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Connection c && c.Guid == Guid && c.Identifier == Identifier;
        }


        /// <summary>
        ///     Starts receiving and sending data.
        /// </summary>
        /// <exception cref="SslException">If SSL is enabled</exception>
        public void Initialize()
        {
            if (Connected)
            {
                return;
            }
            _stream = new NetworkStream(Socket);
            if (SslMode != SslMode.None)
            {
                _sslStream = _sslFactory.Build(this);
                if (SslMode == SslMode.AuthorizationOnly)
                {
                    _sslStream?.Dispose();
                    _sslStream = null;
                }
            }
            Connected = true;
            if (UseCustomIncomingMessageQueue)
            {
                _decoder.PacketReceived = DispatchPayloadReceived;
                CustomIncomingMessageQueue.NewClient();
            }
            if (UseCustomOutcomingMessageQueue)
            {
                CustomOutcomingMessageQueue.NewClient();
            }
            else
            {
                if (!_sendWorkerThreadRunning)
                {
                    _sendWorkerThreadRunning = true;
                    _sendWorkerThread = new Thread(SendWorker) {IsBackground = false};
                    _sendWorkerThread.Start();
                }
            }
            if (!_receiveWorkerThreadRunning)
            {
                _receiveWorkerThreadRunning = true;
#if ReadAsync_Test
                ReadAsync().ConfigureAwait(false);
#else
                _receiveWorkerThread = new Thread(ReadSync) {IsBackground = false};
                _receiveWorkerThread.Start();
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchPayloadReceived(BasicHeader header, object obj, Type type)
        {
            CustomIncomingMessageQueue.Enqueue(new ReceiveMessageQueueItem(OnPayloadReceived, header, obj, type),
                (int) Identifier);
        }

        private void OnDisconnected()
        {
            // top-level logic to run after the connection is closed
            if (UseCustomIncomingMessageQueue)
            {
                CustomIncomingMessageQueue.RemoveClient();
            }
            if (UseCustomOutcomingMessageQueue)
            {
                CustomOutcomingMessageQueue.RemoveClient();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnPayloadReceived(BasicHeader header, object obj, Type type)
        {
            int? unboxedRequest = null;
            if (header.PacketType == PacketType.Trackable)
            {
                if (header is TrackableHeader tHeader)
                {
                    if (header.PacketFlag.HasFlag(PacketFlag.IsRequest))
                    {
                        unboxedRequest = tHeader.RequestId;
                        if (_requestHandlers.TryGetValue(type, out var queue))
                        {
                            var wrapper = new RequestWrapper(this, tHeader.RequestId, obj);
                            lock (queue)
                            {
                                while (queue.Count > 0)
                                {
                                    queue.Dequeue()?.TrySetResult(wrapper);
                                }
                            }
                        }
                    }
                    else if (header.PacketFlag.HasFlag(PacketFlag.IsResponse))
                    {
                        if (_responseHandlers.TryRemove(tHeader.RequestId, out var tcs))
                        {
                            tcs.TrySetResult(obj);
                        }
                    }
                }
            }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            void SendResponse(object o)
            {
                if (o == null)
                {
                    return;
                }
                if (unboxedRequest.HasValue)
                {
                    EnqueueSendResponse(((TrackableHeader) header).RequestId, o);
                }
                else
                {
                    EnqueueSend(o);
                }
            }

            if (_receiveTypeFilters.Count > 0)
            {
                if (_receiveTypeFilters.TryGetValue(type, out var list))
                {
                    lock (list)
                    {
                        foreach (var tcs in list)
                        {
                            tcs.TrySetResult(obj);
                        }
                    }
                }
            }

            if (_receiveFilters.Count > 0)
            {
                lock (_receiveFiltersLock)
                {
                    var enumerator = _receiveFilters.First;
                    while (enumerator != null)
                    {
                        var delete = true;
                        //if (enumerator.Value.Task.IsCompleted || enumerator.Value.Task.IsCanceled) delete = true;
                        if (enumerator.Value.Task.AsyncState is PayloadFilter f)
                        {
                            try
                            {
                                if (f(obj, type))
                                {
                                    enumerator.Value.TrySetResult(obj);
                                }
                                else
                                {
                                    delete = false;
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                        var next = enumerator.Next;
                        if (delete)
                        {
                            _receiveFilters.Remove(enumerator);
                        }
                        enumerator = next;
                    }
                }
            }

            ProcessPayloadHandlers(this, obj, type, SendResponse, unboxedRequest);

            try
            {
                PayloadReceived?.Invoke(this, obj, type);
            }
            catch
            {
                // ignored
            }

            try
            {
                PayloadDispatchHandler?.Invoke(this, obj, type, SendResponse, unboxedRequest);
            }
            catch
            {
                // ignored
            }

            //TODO: Inconsistencies
            // Order:
            // * async (task) handlers: request/response and receive
            // * local payload handlers (one exception breaks the chain)
            // * local 'global' payload callback
            // * global payload handlers (one exception breaks the chain)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object OnRawDataReceived(int bufferId, int seq, Stream stream)
        {
            //currently rawData points to the internal memory stream.
            stream.Seek(0, SeekOrigin.Begin);

            void SendResponse(object o)
            {
                if (o == null)
                {
                    return;
                }
                EnqueueSend(o);
            }

            if (_rawDataHandlers.Count > 0)
            {
                if (_rawDataHandlers.ContainsKey(bufferId))
                {
                    var list = _rawDataHandlers[bufferId];
                    lock (list)
                    {
                        foreach (var handler in list)
                        {
                            SendResponse(handler?.Invoke(bufferId, seq, stream));
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }
            }
            SendResponse(RawDataReceived?.Invoke(bufferId, seq, stream));
            stream.Seek(0, SeekOrigin.Begin);
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendWorker()
        {
            _sendWorkerThreadRunning = true;
            while (Connected)
            {
                if (_sendQueue.Count > 0)
                {
                    if (!_sendQueue.TryDequeue(out var tcs))
                    {
                        continue;
                    }
                    try
                    {
                        PushSendSync(tcs);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                }
                else
                {
                    _sendWorkerWaitHandle.WaitOne();
                }
            }
            _sendWorkerThreadRunning = false;
        }

        private void ReadSync()
        {
            _receiveLock.Wait();
            while (_receiveWorkerThreadRunning)
            {
                try
                {
                    var read = Stream.Read(_readBuffer.Buffer, _readBuffer.Offset, _readBuffer.Capacity);

                    if (read == 0)
                    {
                        try
                        {
                            //_socket.Dispose();
                            Close();
                            //Close();
                        }
                        catch
                        {
                            // ignored
                        }
                        goto CLEANUP;
                    }

                    LastReceived = DateTime.Now;
                    _readBuffer.BytesTransferred = read;
                    _readBuffer.Offset = _readBuffer.BaseOffset;
                    _readBuffer.Count = read;

                    try
                    {
                        if (_readBuffer.Count > 0)
                        {
                            _decoder.ProcessReadBytes(_readBuffer);
                        }
                    }
                    catch (Exception exception)
                    {
                        HandleRemoteDisconnect(SocketError.SocketError, exception);
                    }
                }
                catch (Exception e)
                {
                    HandleRemoteDisconnect(SocketError.SocketError, e);
                }
            }
            CLEANUP:

            _receiveLock.Release();
        }

        private async Task ReadAsync()
        {
            await _receiveLock.WaitAsync();
            while (_receiveWorkerThreadRunning)
            {
                try
                {
                    var read = await Stream.ReadAsync(_readBuffer.Buffer, _readBuffer.Offset, _readBuffer.Capacity).ConfigureAwait(false);

                    if (read == 0)
                    {
                        try
                        {
                            //_socket.Dispose();
                            Close();
                            //Close();
                        }
                        catch
                        {
                            // ignored
                        }
                        goto CLEANUP;
                    }

                    LastReceived = DateTime.Now;
                    _readBuffer.BytesTransferred = read;
                    _readBuffer.Offset = _readBuffer.BaseOffset;
                    _readBuffer.Count = read;

                    try
                    {
                        if (_readBuffer.Count > 0)
                        {
                            _decoder.ProcessReadBytes(_readBuffer);
                        }
                    }
                    catch (Exception exception)
                    {
                        HandleRemoteDisconnect(SocketError.SocketError, exception);
                    }
                }
                catch (Exception e)
                {
                    HandleRemoteDisconnect(SocketError.SocketError, e);
                }
            }
            CLEANUP:

            _receiveLock.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendSync(IPreparedPacket msg)
        {
            if (Socket == null || !Connected)
            {
                throw new SocketException((int) SocketError.NotInitialized);
            }

            _payloadPending = msg;
            _payloadPendingType = msg.GetType();
            //_sendLock.Wait();
            _encoder.Prepare(msg);
            bool isComplete;
            var err = false;
            do
            {
                try
                {
                    _encoder.Send(_writeBuffer);
                    /* Important: this.Stream returns the SSL or basic stream */
                    Stream.Write(_writeBuffer.Buffer, _writeBuffer.Offset, _writeBuffer.Count);
                    isComplete = _encoder.OnSendCompleted(_writeBuffer.Count);
                }
                catch (Exception ex)
                {
                    HandleRemoteDisconnect(SocketError.SocketError, ex);
                    err = true;
                    break;
                }
            } while (!isComplete);

            if (!err)
            {
                //_sendLock.Release();
                _sendCompletionSource?.TrySetResult(_payloadPending);
                PayloadSent?.Invoke(this, _payloadPending, _payloadPendingType);
            }
        }

        /// <summary>
        ///     Sends (pushes) a packet synchronously.
        /// </summary>
        /// <param name="tcs"></param>
        /// <exception cref="SocketException">If the Connection had been closed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PushSendSync(TaskCompletionSource<object> tcs)
        {
            _sendCompletionSource = tcs;
            SendSync(tcs.Task.AsyncState as IPreparedPacket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSendPacket(IPreparedPacket packet)
        {
            if (!Connected)
            {
                throw new InvalidOperationException("The socket had been closed");
            }
            var tcs = new TaskCompletionSource<object>(packet);
            if (UseCustomOutcomingMessageQueue)
            {
                CustomOutcomingMessageQueue.Enqueue(new SendMessageQueueItem(this, tcs), (int) Identifier);
            }
            else
            {
                _sendQueue.Enqueue(tcs);
                _sendWorkerWaitHandle.Set();
            }
            return tcs.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendContent<T>(T payload)
        {
            return EnqueueSendPacket(new PreparedPacket<ContentHeader, T>(new ContentHeader(), payload));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSend<T>(T payload)
        {
            if (payload is IPreparedPacket p)
            {
                return EnqueueSendPacket(p);
            }
            return EnqueueSendContent(payload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Send<T>(T payload)
        {
            return EnqueueSendContent(payload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSendResponse<T>(int requestId, T response)
        {
            return EnqueueSendPacket(new TrackablePacket<T>(new TrackableHeader(requestId, PacketFlag.IsResponse),
                response));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSendResponse<T>(TrackableHeader requestHeader, T response)
        {
            return EnqueueSendResponse(requestHeader.RequestId, response);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendRaw(int bufId, int seq, byte[] buf, int count = -1)
        {
            return EnqueueSendPacket(new RawDataPacket(new RawDataHeader(bufId, seq), buf, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendRaw(int bufId, byte[] buf, int count = -1)
        {
            return EnqueueSendRaw(bufId, 0, buf, count);
        }

        /// <summary>
        ///     WARNING: This function uses the specified Stream to stream data.
        ///     If <paramref name="disposeAfterSend" /> is true, the Stream will be disposed after the send operation is completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendRaw(int bufferId, int seq, Stream stream, int count = -1, bool disposeAfterSend = true)
        {
            return EnqueueSendPacket(new RawDataPacket(new RawDataHeader(bufferId, seq, count, disposeAfterSend), stream));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendRaw(int bufferId, Stream stream, int count = -1, bool disposeAfterSend = true)
        {
            return EnqueueSendRaw(bufferId, 0, stream, count, disposeAfterSend);
        }


        public void Close()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignored
            }

            HandleRemoteDisconnect(SocketError.Shutdown, new SocketException((int) SocketError.Shutdown));
            //_closeEvent.Wait(5000);
            //Connected = false;
        }

        private void HandleRemoteDisconnect(SocketError socketError, Exception exception)
        {
            if (!Connected)
            {
                return;
            }
            Connected = false;
            try
            {
                if (Client?.Connected ?? false)
                {
                    Client?.Client?.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
                // ignored
            }

            _receiveWorkerThreadRunning = _sendWorkerThreadRunning = false;

            ClearHandlers(exception);
            _sendWorkerWaitHandle?.Set();
            OnDisconnected();
            Disconnected?.Invoke(this, exception);

            Cleanup();
        }

        private void ClearHandlers(Exception exception)
        {
            foreach (var t in _responseHandlers)
            {
                t.Value.TrySetException(exception);
            }
            _responseHandlers.Clear();

            foreach (var kv in _requestHandlers)
            {
                var queue = kv.Value;
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        queue.Dequeue()?.TrySetException(exception);
                    }
                }
            }

            lock (_receiveFiltersLock)
            {
                foreach (var t in _receiveFilters)
                {
                    t.TrySetException(exception);
                }
                _receiveFilters.Clear();
            }

            foreach (var kv in _receiveTypeFilters)
            {
                lock (kv.Value)
                {
                    foreach (var t in kv.Value)
                    {
                        t.TrySetException(exception);
                    }
                }
            }

            while (_sendQueue.TryDequeue(out var s))
            {
                s.TrySetException(exception);
            }

            _rawDataHandlers.Clear();
        }

        /// <summary>
        ///     Cleanup this Connection object.
        /// </summary>
        /// <remarks>
        ///     <para>The Connection object, just like <see cref="TcpClient" />, cannot be reused.</para>
        /// </remarks>
        private void Cleanup()
        {
            _encoder?.Clear();
            _decoder?.Clear();
            Client?.Dispose();
            Client = null;
            _stream?.Dispose();
            _sslStream?.Dispose();
            _writeBuffer = _readBuffer = null;
            _payloadPending = _payloadPendingType = null;
            _sendCompletionSource = null;
            Data?.Clear();

            Connected = false;
            if (_sendLock.CurrentCount == 0)
            {
                _sendLock.Release();
            }
            if (_closeEvent.CurrentCount == 1)
            {
                _closeEvent.Wait();
            }
            _sendWorkerThreadRunning = false;
            _receiveWorkerThreadRunning = false;
        }


        public Task CloseAsync()
        {
            Socket.Shutdown(SocketShutdown.Send);
            var t = _closeEvent.WaitAsync(5000);

            // release again so that we can take reuse it internally
            t.ContinueWith(x =>
            {
                _closeEvent.Release();
                Connected = false;
            });

            return t;
        }

        /// <summary>
        ///     Returns the first packet for which <b>filter</b> yields true
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<object> Receive(PayloadFilter filter, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<object>(filter);

            lock (_receiveFiltersLock)
            {
                _receiveFilters.AddLast(tcs);
            }

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t => ((TaskCompletionSource<object>) t).TrySetCanceled(), tcs);
            }
            return tcs.Task;
        }

        public async Task<T> Receive<T>(TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<object>();
            if (!_receiveTypeFilters.TryAddLast(typeof(T), tcs))
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

        public Task<object> SendReceive<TSend>(TSend obj, PayloadFilter filter, TimeSpan? timeout = null)
        {
            var res = Receive(filter, timeout);
            Send(obj);
            return res;
        }

        public Task<TReceive> SendReceive<TSend, TReceive>(TSend obj, TimeSpan? timeout = null)
        {
            var res = Receive<TReceive>(timeout);
            Send(obj);
            return res;
        }

        public Task<object> SendRequest<TRequest>(TRequest req, TimeSpan? timeout = null)
        {
            var id = Interlocked.Increment(ref _lastRequestId);
            var tcs = new TaskCompletionSource<object>(id);

            if (!_responseHandlers.TryAdd(id, tcs))
            {
                throw new InvalidOperationException();
            }

            EnqueueSendPacket(new TrackablePacket<TRequest>(new TrackableHeader(id, PacketFlag.IsRequest), req));

            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t =>
                {
                    var task = (TaskCompletionSource<object>) t;
                    task.TrySetCanceled();
                    _responseHandlers.TryRemove((int) task.Task.AsyncState, out _);
                }, tcs);
            }
            return tcs.Task;
        }

        public Task<RequestWrapper> ReceiveRequest(Type type, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<RequestWrapper>();

            if (!_requestHandlers.TryEnqueue(type, tcs))
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
        public Task<RequestWrapper> ReceiveRequest<T>(TimeSpan? timeout = null)
        {
            return ReceiveRequest(typeof(T), timeout);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns>Return value of non-generic SendRequest casted to TResponse</returns>
        public async Task<TResponse> SendRequest<TRequest, TResponse>(TRequest req, TimeSpan? timeout = null)
        {
            var res = await SendRequest(req, timeout);
            return (TResponse) res;
        }

        public void AppendIncomingRawDataHandler(int bufId, RawDataHandler handler)
        {
            if (!_rawDataHandlers.ContainsKey(bufId))
            {
                _rawDataHandlers.TryAdd(bufId, new LinkedList<RawDataHandler>());
            }
            lock (_rawDataHandlers[bufId])
            {
                _rawDataHandlers[bufId].AddLast(handler);
            }
        }

        public bool RemoveIncomingRawDataHandler(int bufId, RawDataHandler handler)
        {
            if (!_rawDataHandlers.ContainsKey(bufId))
            {
                return false;
            }
            bool ret;
            lock (_rawDataHandlers[bufId])
            {
                ret = _rawDataHandlers[bufId].Remove(handler);
            }
            return ret;
        }

        /// <summary>
        ///     Returns a newly created, unique buffer ID for raw data transfer.
        /// </summary>
        /// <returns>Buffer ID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CreateNewRawDataBuffer()
        {
            return Interlocked.Increment(ref _lastRawDataBufferId);
        }

        /// <summary>
        ///     Removes all RawDataHandlers for the specified buffer ID.
        /// </summary>
        /// <param name="bufId"></param>
        /// <returns>Whether at least one handler has been removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DestroyRawDataBuffer(int bufId)
        {
            return _rawDataHandlers.TryRemove(bufId, out _);
        }

        public static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm,
                stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }

        public static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }

        public static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }

        public static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            var localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0}.",
                    localCertificate.Subject);
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            var remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to.",
                    remoteCertificate.Subject);
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }

        internal delegate void InternalPayloadDispatchHandler(Connection connection, object payload, Type type,
            Action<object> responseSender, int? requestId);

        #region GRAVEYARD

        /*protected virtual async void ReadAsync()
        {
            try
            {
                
                var isPending = Client.Client.ReceiveAsync(_readArgs);
                if (!isPending)
                {
                    OnReadCompleted(Client, _readArgs);
                }
            }
            catch (SocketException e)
            {
                HandleRemoteDisconnect(e.SocketErrorCode, e);
            }
            catch (Exception e)
            {
                HandleRemoteDisconnect(SocketError.SocketError, e);
            }
            try
            {
                int read = await _stream.ReadAsync(_readBuffer.Buffer, _readBuffer.Offset, _readBuffer.Capacity);
                OnReadCompleted(read);
            }
            catch (Exception e)
            {
                HandleRemoteDisconnect(SocketError.SocketError, e);
            }

        }

        private void OnReadCompleted(int read)
        {
            LastReceived = DateTime.Now;
            if (read == 0)
            {
                try
                {
                    //_socket.Dispose();
                    Close();
                    //Close();
                }
                catch { }
                //HandleRemoteDisconnect(e.SocketError, new SocketException((int)e.SocketError));
                return;
            }

            _readBuffer.BytesTransferred = read;
            _readBuffer.Offset = _readBuffer.BaseOffset;
            _readBuffer.Count = read;

            try
            {
                // pre processor can have read everything
                if (_readBuffer.Count > 0)
                    _decoder.ProcessReadBytes(_readBuffer);
            }
            catch (Exception exception)
            {
                HandleRemoteDisconnect(SocketError.SocketError, exception);
                //ChannelFailure(this, exception);

                // Cleanup before both pending operations have exited.
                try
                {
                    if (!Socket.Connected)
                        return;
                }
                catch (NullReferenceException)
                {
                    //rare case of race condition during cleanup.
                    return;
                }
            }
            ReadAsync();
        }
        */

        /*
        protected virtual async void Send(IPreparedPacket msg)
        {
            if (Socket == null || !Socket.Connected)
                throw new SocketException((int)SocketError.NotInitialized);

            _payloadPending = msg;
            _payloadPendingType = msg.GetType();
            await _sendLock.WaitAsync();
            //lock (_writeLock)
            {
                _encoder.Prepare(msg);
                _encoder.Send(_writeBuffer);
                try
                {
                    await _stream.WriteAsync(_writeBuffer.Buffer, _writeBuffer.Offset, _writeBuffer.Count);
                    OnSendCompleted();
                }
                catch (Exception ex)
                {
                    HandleRemoteDisconnect(SocketError.SocketError, ex);
                }
            }

        }

        private async void OnSendCompleted()
        {

            try
            {
                var isComplete = _encoder.OnSendCompleted(_writeBuffer.Count);
                if (!isComplete)
                {
                    _encoder.Send(_writeBuffer);
                    await _stream.WriteAsync(_writeBuffer.Buffer, _writeBuffer.Offset, _writeBuffer.Count);
                    OnSendCompleted();
                    return;
                }

                _sendLock.Release();
                _sendCompletionSource?.SetResult(_payloadPending);
                PayloadSent?.Invoke(this, _payloadPending, _payloadPendingType);
            }
            catch (Exception ex)
            {
                //_sendLock.Release();
                //OnChannelFailure(ex);
                HandleRemoteDisconnect(SocketError.TimedOut, ex);
            }
        }
        */

        #endregion

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Cleanup();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Connection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}

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
using Ace.Networking.Helpers;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.MicroProtocol.Structures;
using Ace.Networking.Serializers;
using Ace.Networking.Services;
using Ace.Networking.Structures;
using Ace.Networking.Threading;
using Ace.Networking.Threading.Workers;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public sealed class Connection : PayloadHandlerDispatcher, IDisposable, IConnection
    {
        public delegate void DisconnectHandler(IConnection connection, Exception exception);

        public delegate bool InternalPayloadDispatchHandler(IConnection connection, object payload, Type type,
            Action<object> responseSender, int? requestId);

        public delegate bool PayloadFilter(object payload, Type type);

        public const int BufferSize = 8192;

        internal static long _lastId = 1;
        internal static int _lastRawDataBufferId = 1;
        internal static int _lastRequestId = 1;
        internal readonly SemaphoreSlim _closeEvent = new SemaphoreSlim(0, 1);

        internal readonly IPayloadDecoder _decoder;
        internal readonly IPayloadEncoder _encoder;

        internal readonly ConcurrentDictionary<int, LinkedList<RawDataHeader.RawDataHandler>> _rawDataHandlers =
            new ConcurrentDictionary<int, LinkedList<RawDataHeader.RawDataHandler>>();


        internal readonly SemaphoreSlim _receiveLock = new SemaphoreSlim(1, 1);

        internal readonly ConcurrentDictionary<int, TaskCompletionSource<object>> _responseTasks =
            new ConcurrentDictionary<int, TaskCompletionSource<object>>();

        internal readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        internal readonly ConcurrentQueue<TaskCompletionSource<object>> _sendQueue =
            new ConcurrentQueue<TaskCompletionSource<object>>();

        private readonly AutoResetEvent _sendWorkerWaitHandle = new AutoResetEvent(false);

        private readonly IInternalServiceManager<IConnection> _services;
        private readonly ISslStreamFactory _sslFactory;

        private object _payloadPending;
        private Type _payloadPendingType;

        private SocketBuffer _readBuffer;
        private Thread _receiveWorkerThread;
        private volatile bool _receiveWorkerThreadRunning;


        private Thread _sendWorkerThread;
        private volatile bool _sendWorkerThreadRunning;

        private Stream _sslStream;

        private Stream _stream;
        private SocketBuffer _writeBuffer;

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
        }


        internal Connection(TcpClient client, ProtocolConfiguration configuration,
            IInternalServiceManager<IConnection> services = null, ISslStreamFactory sslFactory = null,
            IConnectionData data = null, InternalPayloadDispatchHandler dispatcher = null) : this(
            configuration.PayloadEncoder.Clone(), configuration.PayloadDecoder.Clone())
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (client == null || !client.Connected)
                throw new ArgumentException(nameof(client));

            Client = client;

            SslMode = configuration.SslMode;
            CustomOutcomingMessageQueue = configuration.CustomOutcomingMessageQueue;
            CustomIncomingMessageQueue = configuration.CustomIncomingMessageQueue;
            _services = services; // ?? BasicServiceManager<IConnection>.Empty;
            _sslFactory = sslFactory;
            Data = data;
            DispatchPayload = dispatcher;
        }

        public bool UseCustomOutcomingMessageQueue => CustomOutcomingMessageQueue != null;
        public bool UseCustomIncomingMessageQueue => CustomIncomingMessageQueue != null;

        public ThreadedQueueProcessor<ReceiveMessageQueueItem> CustomIncomingMessageQueue { get; }
        public ThreadedQueueProcessor<SendMessageQueueItem> CustomOutcomingMessageQueue { get; }
        public Socket Socket => Client?.Client;
        public Stream Stream => _sslStream ?? _stream;
        public SslMode SslMode { get; }

        public int MessagesQueued
        {
            get
            {
                if (UseCustomOutcomingMessageQueue)
                    throw new NotSupportedException("Connection-binding in custom queue is not supported");
                return _sendQueue.Count;
            }
        }

        public ISslCertificatePair SslCertificates { get; private set; }

        public event InternalPayloadDispatchHandler DispatchPayload;

        public IPayloadSerializer Serializer => _encoder?.Serializer ?? _decoder?.Serializer;
        public ITypeResolver TypeResolver => Serializer.TypeResolver;
        public IServiceManager Services => _services;

        public long Identifier { get; }
        public Guid Guid { get; }
        public bool Connected { get; private set; }

        public IConnectionData Data { get; set; }


        /// <summary>
        ///     Gets the <code>DateTime</code> of the last received packet.
        /// </summary>
        public DateTime LastReceived { get; internal set; }

        public event DisconnectHandler ClientDisconnected;


        /// <summary>
        ///     Starts receiving and sending data.
        /// </summary>
        /// <exception cref="SslException">If SSL is enabled</exception>
        public void Initialize()
        {
            if (Connected) return;
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

            var wrapper = _services.Get<IInternalStreamWrapper>();
            _sslStream = wrapper?.Wrap(Stream);

            Connected = true;
            LastReceived = DateTime.Now;
            _services.Attach(this);

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
                    _sendWorkerThread = new Thread(SendWorker) {IsBackground = true};
                    _sendWorkerThread.Start();
                }
            }

            if (!_receiveWorkerThreadRunning)
            {
                _receiveWorkerThreadRunning = true;
#if ReadAsync_Test
                ReadAsync().ConfigureAwait(false);
#else
                _receiveWorkerThread = new Thread(ReadSync) {IsBackground = true};
                _receiveWorkerThread.Start();
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendResponse<T>(int requestId, T response)
        {
            return EnqueueSendPacket(new TrackablePacket<T>(new TrackableHeader(requestId, PacketFlag.IsResponse),
                response));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task Send<T>(T payload)
        {
            return EnqueueSendContent(payload);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="req"></param>
        /// <param name="timeout"></param>
        /// <returns>Return value of non-generic SendRequest casted to TResponse</returns>
        public async Task<TResponse> SendRequest<TRequest, TResponse>(TRequest req,
            CancellationToken? token = null)
        {
            var res = await SendRequest(req, token).ConfigureAwait(false);
            return (TResponse) res;
        }


        public void Close()
        {
            try
            {
                Socket?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignored
            }

            HandleRemoteDisconnect(SocketError.Shutdown, new SocketException((int) SocketError.Shutdown));
            //_closeEvent.Wait(5000);
            //Connected = false;
        }

        /// <summary>
        ///     Catch-all for all payload received.
        ///     This event should be used in a receive-only fashion.
        /// </summary>
        public event GlobalPayloadHandler PayloadReceived;

        public event GlobalPayloadHandler PayloadSent;
        public event RawDataHeader.RawDataHandler RawDataReceived;


        public void OnRaw(int bufId, RawDataHeader.RawDataHandler handler)
        {
            if (!_rawDataHandlers.TryGetValue(bufId, out var list))
                _rawDataHandlers.TryAdd(bufId, list = new LinkedList<RawDataHeader.RawDataHandler>());
            lock (list)
            {
                list.AddLast(handler);
            }
        }

        public bool OffRaw(int bufId, RawDataHeader.RawDataHandler handler)
        {
            if (!_rawDataHandlers.TryGetValue(bufId, out var list)) return false;
            bool ret;
            lock (list)
            {
                ret = list.Remove(handler);
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


        public Task<object> SendReceive<TSend>(TSend obj, PayloadFilter filter,
            CancellationToken? token = null)
        {
            var res = Receive(filter, token);
            Send(obj);
            return res;
        }


        public Task<TReceive> SendReceive<TSend, TReceive>(TSend obj, CancellationToken? token = null)
        {
            var res = Receive<TReceive>(token);
            Send(obj);
            return res;
        }

        public Task<object> SendRequest<TRequest>(TRequest req, CancellationToken? token = null)
        {
            var id = Interlocked.Increment(ref _lastRequestId);
            var tcs = TaskHelper.New<object>(id);

            if (!_responseTasks.TryAdd(id, tcs)) throw new InvalidOperationException();

            EnqueueSendPacket(new TrackablePacket<TRequest>(new TrackableHeader(id, PacketFlag.IsRequest), req));

            token?.Register(t =>
            {
                var task = (TaskCompletionSource<object>) t;
                task.TrySetCanceled();
                _responseTasks.TryRemove((int) task.Task.AsyncState, out _);
            }, tcs);

            return tcs.Task;
        }

        public TcpClient Client { get; private set; }

        ISslCertificatePair ISslContainer.SslCertificates
        {
            get => SslCertificates;
            set => SslCertificates = value;
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Connection c && c.Guid == Guid && c.Identifier == Identifier;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DispatchPayloadReceived(BasicHeader header, object obj, Type type)
        {
            CustomIncomingMessageQueue.Enqueue(new ReceiveMessageQueueItem(OnPayloadReceived, header, obj, type),
                GetHashCode());
        }

        private void OnDisconnected()
        {
            // top-level logic to run after the connection is closed
            if (UseCustomIncomingMessageQueue) CustomIncomingMessageQueue.RemoveClient();
            if (UseCustomOutcomingMessageQueue) CustomOutcomingMessageQueue.RemoveClient();

            _services.Detach(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnPayloadReceived(BasicHeader header, object obj, Type type)
        {
            int? unboxedRequest = null;
            var handled = false;

            if (header.PacketType == PacketType.Trackable)
                if (header is TrackableHeader tHeader)
                {
                    if (header.PacketFlag.HasFlag(PacketFlag.IsRequest))
                        unboxedRequest = tHeader.RequestId;
                    else if (header.PacketFlag.HasFlag(PacketFlag.IsResponse))
                        if (_responseTasks.TryRemove(tHeader.RequestId, out var tcs))
                        {
                            handled = true;
                            tcs.TrySetResult(obj);
                        }
                }

            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            void SendResponse(object o)
            {
                if (o == null)
                    return;

                handled = true;

                if (!Connected)
                    return;
                if (unboxedRequest.HasValue)
                    EnqueueSendResponse(((TrackableHeader)header).RequestId, o);
                else
                    EnqueueSend(o);
            }


            handled |= ProcessPayloadHandlers(this, obj, type, SendResponse, unboxedRequest);

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
                if (DispatchPayload != null)
                    foreach (var handler in DispatchPayload.GetInvocationList())
                        if (handler is InternalPayloadDispatchHandler h)
                            handled |= h.Invoke(this, obj, type, SendResponse, unboxedRequest);
            }
            catch
            {
                // ignored
            }

            if (!handled && unboxedRequest.HasValue && Connected)
                EnqueueSendPacket(new TrackablePacket<object>(
                    new TrackableHeader(unboxedRequest.Value, PacketFlag.IsResponse | PacketFlag.NoContent), null));

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
                if (o == null) return;
                EnqueueSend(o);
            }

            if (_rawDataHandlers.Count > 0)
                if (_rawDataHandlers.TryGetValue(bufferId, out var list))
                    lock (list)
                    {
                        foreach (var handler in list)
                        {
                            SendResponse(handler?.Invoke(bufferId, seq, stream));
                            stream.Seek(0, SeekOrigin.Begin);
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
                if (_sendQueue.Count > 0)
                {
                    if (!_sendQueue.TryDequeue(out var tcs)) continue;
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

            _sendWorkerThreadRunning = false;
        }

        private void ReadSync()
        {
            try
            {
                _receiveLock.Wait();
                while (_receiveWorkerThreadRunning)
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

                            return;
                        }

                        LastReceived = DateTime.Now;
                        _readBuffer.BytesTransferred = read;
                        _readBuffer.Offset = _readBuffer.BaseOffset;
                        _readBuffer.Count = read;

                        try
                        {
                            if (_readBuffer.Count > 0) _decoder.ProcessReadBytes(_readBuffer);
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
            catch { }
            finally
            {

                _receiveLock.Release();
            }
        }

        private async Task ReadAsync()
        {
            await _receiveLock.WaitAsync().ConfigureAwait(false);
            while (_receiveWorkerThreadRunning)
                try
                {
                    var read = await Stream.ReadAsync(_readBuffer.Buffer, _readBuffer.Offset, _readBuffer.Capacity)
                        .ConfigureAwait(false);

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
                        if (_readBuffer.Count > 0) _decoder.ProcessReadBytes(_readBuffer);
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

            CLEANUP:

            _receiveLock.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendSync(IPreparedPacket msg, TaskCompletionSource<object> sendCompletionSource)
        {
            if (Socket == null || !Connected) throw new SocketException((int) SocketError.NotInitialized);

            _payloadPending = msg.GetPayload();
            _payloadPendingType = _payloadPending?.GetType() ?? typeof(object);
            //_sendLock.Wait();
            _encoder.Prepare(msg);
            bool isComplete;
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
                    throw;
                }
            } while (!isComplete);

            sendCompletionSource?.TrySetResult(_payloadPending);
            PayloadSent?.Invoke(this, _payloadPending, _payloadPendingType);
            
        }

        /// <summary>
        ///     Sends (pushes) a packet synchronously.
        /// </summary>
        /// <param name="tcs"></param>
        /// <exception cref="SocketException">If the Connection had been closed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PushSendSync(TaskCompletionSource<object> tcs)
        {
            SendSync(tcs.Task.AsyncState as IPreparedPacket, tcs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSendPacket(IPreparedPacket packet)
        {
            if (!Connected) throw new InvalidOperationException("The socket had been closed");
            var tcs = new TaskCompletionSource<object>(packet, TaskCreationOptions.RunContinuationsAsynchronously);
            if (UseCustomOutcomingMessageQueue)
            {
                CustomOutcomingMessageQueue.Enqueue(new SendMessageQueueItem(this, tcs), GetHashCode());
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
        internal Task EnqueueSendResponse<T>(TrackableHeader requestHeader, T response)
        {
            return EnqueueSendResponse(requestHeader.RequestId, response);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task EnqueueSend<T>(T payload)
        {
            if (payload is IPreparedPacket p)
                return EnqueueSendPacket(p);
            return EnqueueSendContent(payload);
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
        public Task EnqueueSendRaw(int bufferId, int seq, Stream stream, int count = -1,
            bool disposeAfterSend = true)
        {
            return EnqueueSendPacket(new RawDataPacket(new RawDataHeader(bufferId, seq, count, disposeAfterSend),
                stream));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task EnqueueSendRaw(int bufferId, Stream stream, int count = -1,
            bool disposeAfterSend = true)
        {
            return EnqueueSendRaw(bufferId, 0, stream, count, disposeAfterSend);
        }

        private void HandleRemoteDisconnect(SocketError socketError, Exception exception)
        {
            if (!Connected) return;
            Connected = false;
            try
            {
                if (Client?.Connected ?? false) Client?.Client?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignored
            }

            _receiveWorkerThreadRunning = _sendWorkerThreadRunning = false;

            ClearHandlers(exception);
            _sendWorkerWaitHandle?.Set();
            OnDisconnected();
            ClientDisconnected?.Invoke(this, exception);

            Cleanup();
        }

        private void ClearHandlers(Exception exception)
        {
            foreach (var t in _responseTasks)
                t.Value.TrySetException(exception);
            _responseTasks.Clear();

            foreach (var kv in Bindings)
            {
                var binding = kv.Value;
                lock (binding.ReceiveTasks)
                {
                    while (binding.ReceiveTasks.Count > 0) binding.ReceiveTasks.Dequeue()?.TrySetException(exception);
                }

                lock (binding.RequestTasks)
                {
                    while (binding.RequestTasks.Count > 0) binding.RequestTasks.Dequeue()?.TrySetException(exception);
                }
            }

            _rawDataHandlers.Clear();

            RemoveAllHandlers();

            while (_sendQueue.TryDequeue(out var s))
                s.TrySetException(exception);
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
            Data?.Clear();

            Connected = false;
            if (_sendLock.CurrentCount == 0) _sendLock.Release();
            if (_closeEvent.CurrentCount == 1) _closeEvent.Wait();
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

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing) Cleanup();

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
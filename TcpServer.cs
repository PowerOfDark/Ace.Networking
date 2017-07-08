using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using static Ace.Networking.Connection;

namespace Ace.Networking
{
    public class TcpServer : PayloadHandlerDispatcher
    {
        public delegate bool AcceptClientFilter(Connection client);

        public delegate void ClientAcceptedHandler(Connection client);

        public delegate void ReceiveTimeoutHandler(Connection connection);

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        private readonly Timer _timer;
        private TcpListener _listener;
        private Task _listenerTask;
        private Random _random = new Random();
        private TimeSpan? _receiveTimeout;
        private TimeSpan? _receiveTimeoutCheck;
        private volatile bool _shuttingDown;
        protected ISslStreamFactory SslFactory;

        public TcpServer(IPEndPoint endpoint, ProtocolConfiguration configuration)
        {
            Connections = new ConcurrentDictionary<long, Connection>();
            Endpoint = endpoint;
            Configuration = configuration;
            _timer = new Timer(Timer_Tick, null, 0, System.Threading.Timeout.Infinite);
        }

        public TcpServer(IPEndPoint endpoint, ProtocolConfiguration configuration,
            ISslStreamFactory sslFactory) : this(endpoint, configuration)
        {
            SslFactory = sslFactory;
        }

        /// <summary>
        ///     Triggered whenever a new client connects.
        ///     If the delegate returns <code>false</code>, the connection is immediately closed.
        /// </summary>
        /// <remarks>
        ///     <para>This delegate is invoked in a new <code>Task</code>, together with <see cref="ClientAccepted" /></para>
        /// </remarks>
        /// <param name="client"></param>
        /// <returns>Whether or not to accept the client</returns>
        public AcceptClientFilter AcceptClient { get; set; } = client => true;

        /// <summary>
        ///     Specifies the TimeSpan for triggering <see cref="Timeout" />
        /// </summary>
        /// <remarks>
        ///     Each connection will be checked for the timeout twice in the specified TimeSpan
        /// </remarks>
        public TimeSpan? ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                _receiveTimeout = value;
                if (value.HasValue)
                {
                    _receiveTimeoutCheck = TimeSpan.FromMilliseconds(Math.Max(1, value.Value.TotalMilliseconds / 2));
                    _timer.Change(0, System.Threading.Timeout.Infinite);
                }
            }
        }


        public SslMode SslMode => Configuration.SslMode;

        public IPEndPoint Endpoint { get; protected set; }
        public ConcurrentDictionary<long, Connection> Connections { get; }

        public ProtocolConfiguration Configuration { get; protected set; }
        public event ClientAcceptedHandler ClientAccepted;
        public event GlobalPayloadHandler PayloadReceived;
        public event DisconnectHandler ClientDisconnected;

        /// <summary>
        ///     Triggers after a connection has been idle for the specified TimeSpan (<see cref="ReceiveTimeout" />)
        /// </summary>
        /// <remarks>
        ///     <para>This handler is triggered synchronously in a Timer.</para>
        /// </remarks>
        public event ReceiveTimeoutHandler Timeout;

        private void Timer_Tick(object state)
        {
            if (!ReceiveTimeout.HasValue || !_receiveTimeoutCheck.HasValue)
            {
                return;
            }
            var now = DateTime.Now;
            foreach (var connection in Connections)
            {
                if (!connection.Value.Connected)
                {
                    try
                    {
                        connection.Value.Close();
                    }
                    catch
                    {
                        // ignored
                    }
                    /* Removing values while iterating is unsafe,
                     * but it's handled by ConcurrentDictionary */
                    continue;
                }
                if (now.Subtract(connection.Value.LastReceived) >= ReceiveTimeout.Value)
                {
                    Timeout?.Invoke(connection.Value);
                }
            }

            _timer.Change((int) _receiveTimeoutCheck.Value.TotalMilliseconds, System.Threading.Timeout.Infinite);
        }

        public virtual void Start()
        {
            if (Endpoint == null)
            {
                throw new InvalidOperationException("Invalid endpoint");
            }
            if (_listener != null)
            {
                throw new InvalidOperationException("Already listening");
            }
            if (SslMode != SslMode.None && SslFactory == null)
            {
                throw new SslException("Missing SSL certificate", null);
            }

            _shuttingDown = false;
            _listener = new TcpListener(Endpoint);
            _listener.Start();

            _listenerTask = Task.Factory.StartNew(Accept, TaskCreationOptions.LongRunning);
        }

        public virtual void Stop()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException("Server has not been started");
            }
            _shuttingDown = true;
            _timer.Dispose();
            _listener.Stop();
            _listener = null;
            foreach (var connection in Connections)
            {
                try
                {
                    connection.Value?.Close();
                }
                catch
                {
                    // ignored
                }
            }
            Connections.Clear();
        }

        public virtual void Join()
        {
            if (_listenerTask == null || _shuttingDown)
            {
                throw new InvalidOperationException("Server has not been started");
            }
            _listenerTask.GetAwaiter().GetResult();
        }
#pragma warning disable CS4014 // Unneccessary 'await' on new task
        private async void Accept()
        {
            while (!_shuttingDown)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Task.Factory.StartNew(c => OnClientConnect((TcpClient) c), client);
                }
                catch
                {
                    // ignored
                }
            }
        }
#pragma warning restore CS4014

        private void OnClientConnect(TcpClient client)
        {
            var con = new Connection(client, Configuration, SslFactory);
            try
            {
                con.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{con.Identifier}] {ex.Message}");
                con.Close();
                return;
            }

            if (!AcceptClient(con))
            {
                con.Close();
                //con.Cleanup();
                return;
            }


            con.PayloadDispatchHandler += Con_DispatchPayload;
            con.Disconnected += Con_Disconnected;
            con.LastReceived = DateTime.Now;
            Connections.TryAdd(con.Identifier, con);
            OnClientAccepted(con);
        }

        private void OnClientAccepted(Connection connection)
        {
            ClientAccepted?.Invoke(connection);
        }

        private void Con_Disconnected(Connection connection, Exception exception)
        {
            try
            {
                ClientDisconnected?.Invoke(connection, exception);
            }
            catch
            {
                // ignored
            }
            Connections.TryRemove(connection.Identifier, out _);
        }

        private void Con_DispatchPayload(Connection connection, object payload, Type type,
            Action<object> responseSender)
        {
            ProcessPayloadHandlers(connection, payload, type, responseSender);
            try
            {
                PayloadReceived?.Invoke(connection, payload, type);
            }
            catch
            {
                // ignored
            }
            //TODO: Inconsistencies
        }
    }
}
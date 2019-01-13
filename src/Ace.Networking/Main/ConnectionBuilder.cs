using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Services;
using Ace.Networking.Structures;

namespace Ace.Networking
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        private ProtocolConfiguration _config;
        private IConnectionData _data;
        private Connection.InternalPayloadDispatchHandler _dispatcher;
        private IServicesBuilder<IConnection> _services;
        private ISslStreamFactory _sslFactory;

        public IConnectionBuilder UseConfig(ProtocolConfiguration config)
        {
            _config = config;
            return this;
        }

        public IConnectionBuilder UseServices(IServicesBuilder<IConnection> services)
        {
            _services = services;
            return this;
        }

        public IConnectionBuilder UseServices<TBuilder>(Func<TBuilder, IServicesBuilder<IConnection>> config) where TBuilder : IServicesBuilder<IConnection>
        {
            var builder = Activator.CreateInstance<TBuilder>();
            _services = config.Invoke(builder);
            return this;
        }


        public IConnectionBuilder UseData(IConnectionData data)
        {
            _data = data;
            return this;
        }

        public IConnectionBuilder UseSsl(ISslStreamFactory factory)
        {
            _sslFactory = factory;
            return this;
        }

        public IConnectionBuilder UseClientSsl(string targetCommonName,
            X509Certificate2 certificate, SslProtocols protocols)
        {
            if (_config == null) _config = new ProtocolConfiguration();
            this.UseSsl(_config.GetClientSslFactory(targetCommonName, certificate, protocols));
            return this;
        }

        public IConnectionBuilder UseServerSsl(X509Certificate2 certificate, bool useClient, SslProtocols protocols)
        {
            if (_config == null) _config = new ProtocolConfiguration();
            this.UseSsl(_config.GetServerSslFactory(certificate, useClient, protocols));
            return this;
        }

        public IConnection Build(TcpClient client)
        {
            var cfg = _config ?? new ProtocolConfiguration();
            var services = _services?.Build() ?? ServicesManager<IConnection>.Empty;
            IConnectionData data = _data ?? new ConnectionData();
            return new Connection(client, cfg,
                services, _sslFactory,
                data, _dispatcher);
        }

        public IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher)
        {
            _dispatcher += dispatcher;
            return this;
        }

        public IPayloadSerializer GetSerializer()
        {
            if (_config == null) _config = new ProtocolConfiguration();
            return _config.Serializer;
        }

        public ITypeResolver GetTypeResolver()
        {
            if (_config == null) _config = new ProtocolConfiguration();
            return _config.TypeResolver;
        }
    }
}
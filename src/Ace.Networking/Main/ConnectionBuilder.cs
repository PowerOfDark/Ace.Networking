using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.Extensions;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Serializers;
using Ace.Networking.Services;
using Ace.Networking.Structures;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        private ProtocolConfiguration _config;
        private IConnectionData _data;
        private List<Connection.InternalPayloadDispatchHandler> _dispatcher;
        private IServicesBuilder<IConnection> _services;
        private ISslStreamFactory _sslFactory;

        public ConnectionBuilder(ProtocolConfiguration config = null)
        {
            _config = config ?? new ProtocolConfiguration();
            _dispatcher = new List<Connection.InternalPayloadDispatchHandler>(0);
        }

        public IConnectionBuilder UseServices(IServicesBuilder<IConnection> services)
        {
            _services = services;
            return this;
        }

        public IConnectionBuilder UseServices<TBuilder>(Func<TBuilder, IServicesBuilder<IConnection>> config)
            where TBuilder : IServicesBuilder<IConnection>
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
            UseSsl(_config.GetClientSslFactory(targetCommonName, certificate, protocols));
            return this;
        }

        public IConnectionBuilder UseServerSsl(X509Certificate2 certificate, bool useClient, SslProtocols protocols)
        {
            UseSsl(_config.GetServerSslFactory(certificate, useClient, protocols));
            return this;
        }

        public IConnection Build(TcpClient client)
        {
            var cfg = _config ?? new ProtocolConfiguration();
            var services = _services?.Build() ?? ServicesManager<IConnection>.Empty;
            var data = _data ?? new ConnectionData();
            return new Connection(client, cfg,
                services, _sslFactory,
                data, _dispatcher);
        }

        public IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher)
        {
            _dispatcher.Subscribe(dispatcher);
            return this;
        }

        public IPayloadSerializer GetSerializer()
        {
            return _config.Serializer;
        }

        public ITypeResolver GetTypeResolver()
        {
            return _config.TypeResolver;
        }
    }
}
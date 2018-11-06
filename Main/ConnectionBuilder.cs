﻿using System;
using System.Net.Sockets;
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

        public IConnectionBuilder UseServices(Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config)
        {
            return UseServices<ServicesBuilder<IConnection>>(config);
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
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

        public IConnectionBuilder UseServices(Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config)
        {
            _services = config.Invoke(new ServicesBuilder<IConnection>());
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

        public IConnection Build(TcpClient client)
        {
            return new Connection(client, _config ?? new ProtocolConfiguration(),
                _services?.Build() ?? ServicesManager<IConnection>.Empty, _sslFactory,
                _data ?? new ConnectionData(), _dispatcher);
        }

        public IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher)
        {
            _dispatcher += dispatcher;
            return this;
        }
    }
}
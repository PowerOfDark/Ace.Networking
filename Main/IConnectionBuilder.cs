using System;
using System.Net.Sockets;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking
{
    public interface IConnectionBuilder
    {
        IConnectionBuilder UseConfig(ProtocolConfiguration config);
        IConnectionBuilder UseServices(IServicesBuilder<IConnection> services);

        IConnectionBuilder UseServices<TBuilder>(Func<TBuilder, IServicesBuilder<IConnection>> config)
            where TBuilder : IServicesBuilder<IConnection>;

        IConnectionBuilder UseServices(Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config);

        IConnectionBuilder UseSsl(ISslStreamFactory factory);
        IConnectionBuilder UseData(IConnectionData data);
        IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher);

        IPayloadSerializer GetSerializer();
        ITypeResolver GetTypeResolver();

        IConnection Build(TcpClient client);
    }
}
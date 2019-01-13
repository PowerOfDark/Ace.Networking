using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Serializers;
using Ace.Networking.Services;
using Ace.Networking.Structures;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public interface IConnectionBuilder
    {
        IConnectionBuilder UseConfig(ProtocolConfiguration config);
        IConnectionBuilder UseServices(IServicesBuilder<IConnection> services);

        IConnectionBuilder UseServices<TBuilder>(Func<TBuilder, IServicesBuilder<IConnection>> config)
            where TBuilder : IServicesBuilder<IConnection>;

        IConnectionBuilder UseSsl(ISslStreamFactory factory);

        IConnectionBuilder UseClientSsl(string targetCommonName = "",
            X509Certificate2 certificate = null, SslProtocols protocols = SslProtocols.Tls12);

        IConnectionBuilder UseServerSsl(X509Certificate2 certificate = null, bool useClient = true,
            SslProtocols protocols = SslProtocols.Tls12);

        IConnectionBuilder UseData(IConnectionData data);
        IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher);

        IPayloadSerializer GetSerializer();
        ITypeResolver GetTypeResolver();

        IConnection Build(TcpClient client);
    }
}
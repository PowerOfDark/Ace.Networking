using Ace.Networking.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking
{
    public interface IConnectionBuilder
    {
        IConnectionBuilder UseConfig(ProtocolConfiguration config);
        IConnectionBuilder UseServices(IServicesBuilder<IConnection> services);
        IConnectionBuilder UseServices(Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config);
        IConnectionBuilder UseSsl(ISslStreamFactory factory);
        IConnectionBuilder UseData(IConnectionData data);
        IConnectionBuilder UseDispatcher(Connection.InternalPayloadDispatchHandler dispatcher);

        IConnection Build(TcpClient client);
    }
}

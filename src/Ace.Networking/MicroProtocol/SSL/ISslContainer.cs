using System.Net.Sockets;

namespace Ace.Networking.MicroProtocol.SSL
{
    public interface ISslContainer
    {
        TcpClient Client { get; }
        ISslCertificatePair SslCertificates { get; set; }
    }
}
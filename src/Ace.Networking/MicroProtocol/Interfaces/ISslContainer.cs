using System.Net.Sockets;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface ISslContainer
    {
        TcpClient Client { get; }
        ISslCertificatePair SslCertificates { get; set; }
    }
}
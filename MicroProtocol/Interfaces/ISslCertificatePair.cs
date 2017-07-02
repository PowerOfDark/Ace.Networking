using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.SSL;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface ISslCertificatePair
    {
        X509Certificate Certificate { get; }

        BasicCertificateInfo RemoteCertificate { get; }
        SslPolicyErrors RemotePolicyErrors { get; }
    }
}
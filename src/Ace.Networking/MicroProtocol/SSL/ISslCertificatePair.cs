using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Ace.Networking.MicroProtocol.SSL
{
    public interface ISslCertificatePair
    {
        X509Certificate Certificate { get; }

        BasicCertificateInfo RemoteCertificate { get; }
        SslPolicyErrors RemotePolicyErrors { get; }
    }
}
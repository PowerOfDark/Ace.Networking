using Ace.Networking.MicroProtocol.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Ace.Networking.MicroProtocol.SSL
{
    public class SslCertificatePair : ISslCertificatePair
    {
        public X509Certificate Certificate { get; internal set; }

        public BasicCertificateInfo RemoteCertificate { get; internal set; }

        public SslPolicyErrors RemotePolicyErrors { get; internal set; }
    }
}

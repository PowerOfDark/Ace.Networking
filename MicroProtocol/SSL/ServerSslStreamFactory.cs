using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.MicroProtocol.SSL
{
    public class ServerSslStreamFactory : ISslStreamFactory
    {
        public ServerSslStreamFactory(X509Certificate2 certificate, bool useClient = true,
            SslProtocols allowedProtocols = SslProtocols.Tls12)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            Protocols = allowedProtocols;
            UseClientCertificate = useClient;
        }

        /// <summary>
        ///     check if the certificate have been revoked.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        ///     Allowed protocols
        /// </summary>
        public SslProtocols Protocols { get; set; }

        /// <summary>
        ///     The client must supply a certificate
        /// </summary>
        public bool UseClientCertificate { get; set; }

        public SslStream Build(Connection connection)
        {
            var stream = new SslStream(connection.Client.GetStream(), true, OnRemoteCertificateValidation);

            try
            {
                var task = stream.AuthenticateAsServerAsync(Certificate, UseClientCertificate, Protocols,
                    CheckCertificateRevocation);
                task.Wait();
            }
            catch (IOException err)
            {
                throw new SslException("Failed to authenticate " + connection.Socket.RemoteEndPoint, err);
            }
            catch (ObjectDisposedException err)
            {
                throw new SslException("Failed to create stream, did client disconnect directly?", err);
            }
            catch (AuthenticationException err)
            {
                throw new SslException("Failed to authenticate " + connection.Socket.RemoteEndPoint, err);
            }

            return stream;
        }

        /// <summary>
        ///     Certificate to use in this server.
        /// </summary>
        public X509Certificate Certificate { get; }

        public BasicCertificateInfo RemoteCertificate { get; protected set; }

        public SslPolicyErrors RemotePolicyErrors { get; protected set; }

        protected virtual bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslpolicyerrors)
        {
            RemoteCertificate = new BasicCertificateInfo(certificate);
            RemotePolicyErrors = sslpolicyerrors;
            if (UseClientCertificate)
            {
                if (sslpolicyerrors != SslPolicyErrors.None)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
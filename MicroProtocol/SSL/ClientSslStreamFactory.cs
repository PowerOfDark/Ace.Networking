using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.MicroProtocol.SSL
{
    /// <summary>
    ///     Builder used to create SslStreams for client side applications.
    /// </summary>
    public class ClientSslStreamFactory : ISslStreamFactory
    {
        /// <summary>
        /// </summary>
        /// <param name="commonName">the domain name of the server that you are connecting to</param>
        /// <param name="certificate"></param>
        /// <param name="protocols"></param>
        public ClientSslStreamFactory(string commonName, X509Certificate2 certificate = null,
            SslProtocols protocols = SslProtocols.Tls12)
        {
            CommonName = commonName;
            Protocols = protocols;
            Certificate = certificate;
        }

        /// <summary>
        ///     Typically the domain name of the server that you are connecting to.
        /// </summary>
        public string CommonName { get; }

        /// <summary>
        ///     Allowed SSL protocols
        /// </summary>
        public SslProtocols Protocols { get; set; }

        /// <summary>
        ///     Leave empty to use the server certificate
        /// </summary>
        public X509Certificate Certificate { get; set; }

        public BasicCertificateInfo RemoteCertificate { get; protected set; }

        public SslPolicyErrors RemotePolicyErrors { get; protected set; }

        /// <summary>
        ///     Build a new SSL steam.
        /// </summary>
        /// <returns>Stream which is ready to be used (must have been validated)</returns>
        public SslStream Build(Connection connection)
        {
            var stream = new SslStream(connection.Client.GetStream(), true, OnRemoteCertificateValidation, OnCertificateSelection);

            try
            {
                X509CertificateCollection certificates = null;
                if (Certificate != null)
                {
                    certificates = new X509CertificateCollection(new[] {Certificate});
                }

                var task = stream.AuthenticateAsClientAsync(CommonName, certificates, Protocols, false);
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

        protected X509Certificate OnCertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return ((localCertificates?.Count ?? 0) > 0 ? localCertificates[0] : null) ?? Certificate;
        }


        /// <summary>
        ///     Used to validate the certificate that the server have provided.
        /// </summary>
        /// <param name="sender">Server.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslpolicyerrors">The sslpolicyerrors.</param>
        /// <returns><c>true</c> if the certificate will be allowed, otherwise <c>false</c>.</returns>
        protected virtual bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslpolicyerrors)
        {
            RemoteCertificate = new BasicCertificateInfo(certificate);
            RemotePolicyErrors = sslpolicyerrors;
            return sslpolicyerrors == SslPolicyErrors.None;
            //return (Certificate != null && certificate == null) || (Certificate == null && certificate != null);
        }
    }
}
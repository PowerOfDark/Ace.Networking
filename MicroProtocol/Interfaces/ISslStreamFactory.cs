using System.Net.Security;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface ISslStreamFactory : ISslCertificatePair
    {
        /// <summary>
        ///     Build a new SSL steam.
        /// </summary>
        /// <returns>Stream which is ready to be used (must have been validated)</returns>
        SslStream Build(Connection connection);
    }
}
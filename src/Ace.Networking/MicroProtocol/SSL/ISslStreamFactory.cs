using System.Net.Security;

namespace Ace.Networking.MicroProtocol.SSL
{
    public interface ISslStreamFactory
    {
        /// <summary>
        ///     Build a new SSL steam.
        /// </summary>
        /// <returns>Stream which is ready to be used (must have been validated)</returns>
        SslStream Build(ISslContainer connection);
    }
}
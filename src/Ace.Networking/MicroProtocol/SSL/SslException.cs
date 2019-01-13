using System;

namespace Ace.Networking.MicroProtocol.SSL
{
    public class SslException : Exception
    {
        public SslException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
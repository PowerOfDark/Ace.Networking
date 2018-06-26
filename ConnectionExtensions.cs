using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.PacketTypes;

namespace Ace.Networking
{
    public static class ConnectionExtensions
    {
        public static CancellationToken? GetCancellationToken(this TimeSpan? ts)
        {
            if (ts.HasValue)
                return new CancellationTokenSource(ts.Value).Token;
            return null;
        }

        public static CancellationToken GetCancellationToken(this TimeSpan ts)
        {
            return new CancellationTokenSource(ts).Token;
        }
    }
}
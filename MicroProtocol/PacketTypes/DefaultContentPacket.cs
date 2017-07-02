using System;
using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.MicroProtocol.PacketTypes
{
    public class DefaultContentPacket : PreparedPacket<ContentHeader, object>
    {
        internal DefaultContentPacket(ContentHeader header, object payload) : base(header, payload)
        {
        }

        public Type Type { get; set; }
    }
}
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.MicroProtocol.PacketTypes
{
    public class PreparedPacket<THeader, TPayload> : IPreparedPacket where THeader : BasicHeader
    {
        internal PreparedPacket(THeader header, TPayload payload)
        {
            Header = header;
            Payload = payload;
        }

        public THeader Header { get; set; }
        public TPayload Payload { get; set; }

        public object GetPayload()
        {
            return Payload;
        }

        public BasicHeader GetHeader()
        {
            return Header;
        }
    }
}
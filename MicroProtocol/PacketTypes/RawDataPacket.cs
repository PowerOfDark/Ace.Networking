using System.IO;
using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.MicroProtocol.PacketTypes
{
    public class RawDataPacket : PreparedPacket<RawDataHeader, Stream>
    {
        internal RawDataPacket(RawDataHeader header, byte[] rawData, int len = -1) : this(header,
            new MemoryStream(rawData))
        {
            (Payload as MemoryStream)?.SetLength(len > 0 ? len : rawData.Length);
        }

        internal RawDataPacket(RawDataHeader header, Stream stream) : base(header, stream)
        {
        }
    }
}
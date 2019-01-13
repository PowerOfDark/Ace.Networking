using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.MicroProtocol.PacketTypes
{
    public interface IPreparedPacket
    {
        object GetPayload();

        BasicHeader GetHeader();
    }
}
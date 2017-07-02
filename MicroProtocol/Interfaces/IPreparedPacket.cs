using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPreparedPacket
    {
        object GetPayload();

        BasicHeader GetHeader();
    }
}
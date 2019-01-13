using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.MicroProtocol.PacketTypes
{
    public class TrackablePacket<T> : PreparedPacket<TrackableHeader, T>
    {
        internal TrackablePacket(TrackableHeader header, T payload) : base(header, payload)
        {
        }
    }
}
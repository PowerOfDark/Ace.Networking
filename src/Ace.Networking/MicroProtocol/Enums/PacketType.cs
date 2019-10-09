using ProtoBuf;

namespace Ace.Networking.MicroProtocol.Enums
{
    public enum PacketType
    {
        Unknown = 0,
        ContentPacket,
        RawData,
        Trackable
    }
}
using ProtoBuf;

namespace Ace.Networking.MicroProtocol.Enums
{
    [ProtoContract]
    public enum PacketType
    {
        Unknown = 0,
        ContentPacket,
        RawData,
        Trackable,

    }
}
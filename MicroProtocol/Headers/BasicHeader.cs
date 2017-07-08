using System.Runtime.CompilerServices;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class BasicHeader
    {
        public BasicHeader(PacketType type = PacketType.Unknown)
        {
            PacketType = type;
            PacketFlag = PacketFlag.None;
        }

        public int Position { get; set; }

        public PacketType PacketType { get; set; }
        public PacketFlag PacketFlag { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Serialize(byte[] target, int offset = 0)
        {
            Position = 0;
            target[Position++ + offset] = (byte) PacketType;
            target[Position++ + offset] = (byte) PacketFlag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual BasicHeader Deserialize(byte[] target, int offset = 0)
        {
            Position = 0;
            PacketType = (PacketType) target[Position++ + offset];
            PacketFlag = (PacketFlag) target[Position++ + offset];
            return this;
        }

        public static BasicHeader Upgrade(byte[] target, int offset = 0)
        {
            var type = (PacketType) target[offset];
            BasicHeader upgraded;
            switch (type)
            {
                case PacketType.ContentPacket:
                    upgraded = new ContentHeader();
                    break;
                case PacketType.RawData:
                    upgraded = new RawDataHeader();
                    break;
                case PacketType.Trackable:
                    upgraded = new TrackableHeader();
                    break;
                default:
                    upgraded = new BasicHeader();
                    break;
            }

            return upgraded.Deserialize(target, offset);
        }
    }
}
using System.Runtime.CompilerServices;
using Ace.Networking.Memory;
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


        public PacketType PacketType;// { get; set; }
        public PacketFlag PacketFlag;// { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Serialize(RecyclableMemoryStream target)
        {
            target.WriteByte((byte)PacketType);
            target.WriteByte((byte)PacketFlag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual BasicHeader Deserialize(RecyclableMemoryStream target)
        {
            PacketType = (PacketType)target.ReadByte();
            PacketFlag = (PacketFlag)target.ReadByte();
            return this;
        }

        public static BasicHeader Upgrade(RecyclableMemoryStream target)
        {
            var type = (PacketType)target.ReadByte();
            target.Seek(-1, System.IO.SeekOrigin.Current);
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

            return upgraded.Deserialize(target);
        }
    }
}
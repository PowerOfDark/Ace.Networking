using System;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class TrackableHeader : ContentHeader
    {
        public TrackableHeader(int requestId, PacketFlag flag) : base(PacketType.Trackable)
        {
            PacketFlag = flag;
            RequestId = requestId;
        }

        public TrackableHeader() : base(PacketType.Trackable)
        {
        }

        public int RequestId { get; set; }

        public override BasicHeader Deserialize(byte[] target, int offset = 0)
        {
            base.Deserialize(target, offset);
            RequestId = BitConverter.ToInt32(target, Position + offset);
            Position += sizeof(int);
            return this;
        }

        public override void Serialize(byte[] target, int offset = 0)
        {
            base.Serialize(target, offset);
            BitConverter2.GetBytes(RequestId, target, offset + Position);
            Position += sizeof(int);
        }
    }
}
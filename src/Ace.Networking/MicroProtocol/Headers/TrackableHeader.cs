using System;
using Ace.Networking.Memory;
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

        public override BasicHeader Deserialize(RecyclableMemoryStream target)
        {
            base.Deserialize(target);
            RequestId = target.ReadInt32();
            return this;
        }

        public override void Serialize(RecyclableMemoryStream target)
        {
            base.Serialize(target);
            target.Write(RequestId);
        }
    }
}
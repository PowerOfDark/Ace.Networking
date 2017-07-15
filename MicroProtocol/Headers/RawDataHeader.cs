using System;
using System.IO;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class RawDataHeader : BasicHeader
    {
        public delegate object RawDataHandler(int bufferId, int seq, Stream stream);

        public RawDataHeader(int rawDataBufferId, int rawDataSeq, int byteCount = -1, bool disposeStreamAfterSend = true) : base(PacketType.RawData)
        {
            RawDataBufferId = rawDataBufferId;
            RawDataSeq = rawDataSeq;
            ContentLength = byteCount;
            DisposeStreamAfterSend = disposeStreamAfterSend;
        }

        public RawDataHeader()
        {
        }

        public int RawDataBufferId { get; set; }
        public int RawDataSeq { get; set; }
        public int ContentLength { get; set; }

        //[NotMapped]
        internal bool DisposeStreamAfterSend { get; set; }

        public override void Serialize(byte[] target, int offset = 0)
        {
            base.Serialize(target, offset);
            Position += BitHelper.WriteInt(target, Position + offset, RawDataBufferId, RawDataSeq, ContentLength);
        }

        public override BasicHeader Deserialize(byte[] target, int offset = 0)
        {
            base.Deserialize(target, offset);
            RawDataBufferId = BitConverter.ToInt32(target, offset + Position);
            Position += sizeof(int);
            RawDataSeq = BitConverter.ToInt32(target, offset + Position);
            Position += sizeof(int);
            ContentLength = BitConverter.ToInt32(target, offset + Position);
            Position += sizeof(int);

            return this;
        }
    }
}
using System.IO;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class RawDataHeader : BasicHeader
    {
        public delegate object RawDataHandler(int bufferId, int seq, Stream stream);

        public RawDataHeader(int rawDataBufferId, int rawDataSeq, int byteCount = -1,
            bool disposeStreamAfterSend = true) : base(PacketType
            .RawData)
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

        public override void Serialize(RecyclableMemoryStream target)
        {
            base.Serialize(target);
            target.Write7BitInt(RawDataBufferId);
            target.Write7BitInt(RawDataSeq);
            target.Write7BitInt(ContentLength);
        }

        public override BasicHeader Deserialize(RecyclableMemoryStream target)
        {
            base.Deserialize(target);
            RawDataBufferId = target.Read7BitInt();
            RawDataSeq = target.Read7BitInt();
            ContentLength = target.Read7BitInt();

            return this;
        }
    }
}
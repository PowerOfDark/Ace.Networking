using System;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class ContentHeader : BasicHeader
    {
        public ContentHeader(PacketType type = PacketType.ContentPacket) : base(type)
        {
        }

        public byte[] ContentType { get; set; }

        public ushort ContentTypeLength => checked((ushort) (ContentType?.Length ?? 0));

        public int ContentLength { get; set; }


        public override BasicHeader Deserialize(RecyclableMemoryStream target)
        {
            base.Deserialize(target);
            var contentTypeLength = target.ReadUInt16();
            ContentType = new byte[contentTypeLength];
            target.Read(ContentType, 0, contentTypeLength);
            ContentLength = target.ReadInt32();

            return this;
        }

        public override void Serialize(RecyclableMemoryStream target)
        {
            base.Serialize(target);

            target.Write((short)ContentTypeLength);

            target.Write(ContentType, 0, ContentTypeLength);
            target.Write(ContentLength);
        }
    }
}
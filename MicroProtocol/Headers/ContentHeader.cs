using System;
using Ace.Networking.MicroProtocol.Enums;

namespace Ace.Networking.MicroProtocol.Headers
{
    public class ContentHeader : BasicHeader
    {
        public ContentHeader(PacketType type = PacketType.ContentPacket) : base(type)
        {
        }

        public byte[] ContentType { get; set; }
        public ushort ContentTypeLength => (ushort)(ContentType?.Length ?? 0);

        public int ContentLength { get; set; }


        public override BasicHeader Deserialize(byte[] target, int offset = 0)
        {
            base.Deserialize(target, offset);
            var contentTypeLength = BitConverter.ToUInt16(target, offset + Position);
            Position += sizeof(ushort);
            ContentType = new byte[contentTypeLength];
            for (var i = 0; i < contentTypeLength; i++)
            {
                ContentType[i] = target[offset + Position++];
            }
            ContentLength = BitConverter.ToInt32(target, offset + Position);
            Position += sizeof(int);

            return this;
        }

        public override void Serialize(byte[] target, int offset = 0)
        {
            base.Serialize(target, offset);
            BitConverter2.GetBytes((short)ContentTypeLength, target, offset + Position);
            Position += sizeof(ushort);
            //Encoding.ASCII.GetBytes(ContentType, 0, ContentTypeLength, target, offset + Position); Position += ContentTypeLength;
            for (var i = 0; i < ContentTypeLength; i++)
            {
                target[offset + Position++] = ContentType[i];
            }
            BitConverter2.GetBytes(ContentLength, target, offset + Position);
            Position += sizeof(int);
        }
    }
}
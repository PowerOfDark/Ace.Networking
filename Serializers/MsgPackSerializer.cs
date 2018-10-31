using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using MessagePack;
using MessagePack.Resolvers;

namespace Ace.Networking.Serializers
{
    public class MsgPackSerializer : PayloadSerializerBase
    {
        public MsgPackSerializer(ITypeResolver typeResolver) : base(typeResolver)
        {
        }

        public override byte[] SupportedContentType => new byte[] {0x4F};
        public override object DeserializeType(Type type, Stream source)
        {
            return MessagePackSerializer.NonGeneric.Deserialize(type, source);
        }

        public override void Serialize(object source, Stream destination)
        {
            MessagePackSerializer.NonGeneric.Serialize(source.GetType(), destination, source,
                ContractlessStandardResolver.Instance);
        }

        public override IPayloadSerializer Clone()
        {
            return new MsgPackSerializer(this.TypeResolver);
        }
    }
}

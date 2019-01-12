using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using ProtoBuf;

namespace Ace.Networking.Serializers.Protobuf
{
    public class ProtobufSerializer : PayloadSerializerBase
    {
        public override byte[] SupportedContentType => new byte[] {0x13};

        public override IPayloadSerializer Clone()
        {
            return this;
        }

        public override object DeserializeType(Type type, Stream source)
        {
            return Serializer.NonGeneric.Deserialize(type, source);
        }

        public override void SerializeContent(object source, Stream destination)
        {
            Serializer.NonGeneric.Serialize(destination, source);
        }

        public ProtobufSerializer(ITypeResolver typeResolver) : base(typeResolver)
        {
        }
    }
}

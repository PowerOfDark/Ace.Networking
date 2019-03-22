using System;
using System.IO;
using Ace.Networking.TypeResolvers;
using ProtoBuf;

namespace Ace.Networking.Serializers
{
    public class ProtobufSerializer : PayloadSerializerBase
    {
        public ProtobufSerializer(ITypeResolver typeResolver) : base(typeResolver)
        {
        }

        public override byte[] SupportedContentType => new byte[] {0x13};

        public override IPayloadSerializer Clone()
        {
            return this;
        }

        public override object DeserializeType(Type type, Stream source)
        {
            if (type == typeof(object)) return null;
            return Serializer.NonGeneric.Deserialize(type, source);
        }

        public override void SerializeContent(object source, Stream destination)
        {
            Serializer.NonGeneric.Serialize(destination, source);
        }
    }
}
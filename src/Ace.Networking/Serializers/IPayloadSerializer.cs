using System;
using System.IO;
using System.Reflection;

namespace Ace.Networking.Serializers
{
    public interface IPayloadSerializer
    {
        byte[] SupportedContentType { get; }
        ITypeResolver TypeResolver { get; }
        object Deserialize(byte[] contentType, Stream source, out Type resolvedType);
        object DeserializeType(Type type, Stream source);

        void Serialize(object source, Stream destination, out byte[] contentType);
        void SerializeContent(object source, Stream destination);

        IPayloadSerializer Clone();

        bool IsValidContentType(byte[] contentType);
    }
}
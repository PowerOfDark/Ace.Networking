using System;
using System.IO;
using System.Reflection;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadSerializer
    {
        byte[] SupportedContentType { get; }

        object Deserialize(byte[] contentType, Stream source, out Type resolvedType);
        object DeserializeType(Type type, Stream source);

        void Serialize(object source, Stream destination, out byte[] contentType);
        void Serialize(object source, Stream destination);

        IPayloadSerializer Clone();

        bool IsValidContentType(byte[] contentType);

        byte[] CreateContentType(Type type);

        void RegisterAssembly(Assembly assembly);
    }
}
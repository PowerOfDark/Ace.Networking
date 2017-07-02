using System;
using System.IO;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadSerializer
    {
        byte[] SupportedContentType { get; }

        object Deserialize(byte[] contentType, Stream source, out Type resolvedType);

        void Serialize(object source, Stream destination, out byte[] contentType);

        IPayloadSerializer Clone();

        bool IsValidContentType(byte[] contentType);

        byte[] CreateContentType(Type type);
    }
}
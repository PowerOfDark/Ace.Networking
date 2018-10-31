using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Serializers
{
    public abstract class PayloadSerializerBase : IPayloadSerializer
    {
        public static readonly byte[] NullSerializer = {0x0};

        public abstract byte[] SupportedContentType { get; }
        public ITypeResolver TypeResolver { get; set; }

        public PayloadSerializerBase(ITypeResolver typeResolver)
        {
            this.TypeResolver = typeResolver;
        }

        public virtual object Deserialize(byte[] contentType, Stream source, out Type resolvedType)
        {
            if (!IsValidContentType(contentType)) throw new InvalidDataException(nameof(contentType));
            if (!TypeResolver.TryResolve(source, out resolvedType)) throw new InvalidDataException("type");
            return DeserializeType(resolvedType, source);
        }

        public abstract object DeserializeType(Type type, Stream source);

        public virtual void Serialize(object source, Stream destination, out byte[] contentType)
        {
            contentType = SupportedContentType;
            if(!TypeResolver.TryWrite(destination, source?.GetType() ?? typeof(object))) throw new InvalidOperationException($"no type resolver can handle the specified type");
            Serialize(source, destination);
        }

        public abstract void Serialize(object source, Stream destination);

        public abstract IPayloadSerializer Clone();

        public virtual bool IsValidContentType(byte[] contentType)
        {
            return Enumerable.SequenceEqual(SupportedContentType, contentType);
        }
    }
}

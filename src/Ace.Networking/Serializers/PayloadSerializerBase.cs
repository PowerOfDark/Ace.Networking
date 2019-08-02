using System;
using System.IO;
using Ace.Networking.Interfaces;
using Ace.Networking.Threading;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking.Serializers
{
    public abstract class PayloadSerializerBase : IPayloadSerializer
    {
        public static readonly byte[] NullSerializer = { 0x0 };

        public PayloadSerializerBase(ITypeResolver typeResolver)
        {
            TypeResolver = typeResolver;
        }

        public ITypeResolver TypeResolver { get; set; }

        public abstract byte[] SupportedContentType { get; }

        public virtual object Deserialize(byte[] contentType, Stream source, out Type resolvedType)
        {
            if (!IsValidContentType(contentType)) throw new InvalidDataException(nameof(contentType));
            if (!TypeResolver.TryResolve(source, out resolvedType)) throw new InvalidDataException("type");
            var ret = DeserializeType(resolvedType, source);
            if (ret is ISerializationListener l) l.PostDeserialize(this, source);
            return ret;
        }

        public abstract object DeserializeType(Type type, Stream source);

        public virtual void Serialize(object source, Stream destination, out byte[] contentType)
        {
            contentType = SupportedContentType;
            if (!TypeResolver.TryWrite(destination, source?.GetType() ?? typeof(object)))
                throw new InvalidOperationException($"no type resolver can handle the specified type");
            var l = source as ISerializationListener;
            l?.PreSerialize(this, destination);
            SerializeContent(source, destination);
            l?.PostSerialize(this, destination);
        }

        public abstract void SerializeContent(object source, Stream destination);

        public abstract IPayloadSerializer Clone();

        public virtual bool IsValidContentType(byte[] contentType)
        {
            return contentType == SupportedContentType || SequenceEqual(SupportedContentType, contentType);
        }

        private bool SequenceEqual(byte[] supportedContentType, byte[] contentType)
        {
            if (supportedContentType.Length != contentType.Length) return false;
            for (var i = 0; i < supportedContentType.Length; i++)
                if (SupportedContentType[i] != contentType[i])
                    return false;
            return true;
        }
    }
}
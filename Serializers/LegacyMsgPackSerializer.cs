using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using MessagePack;
using MessagePack.Resolvers;
using ProtoBuf;

namespace Ace.Networking.Serializers
{
    public class LegacyMsgPackSerializer : IPayloadSerializer
    {
        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, byte[]> ContentTypeCache = new Dictionary<Type, byte[]>();

        private static readonly byte[] ContentType = {0x4F, 0x47};

        /// <summary>
        ///     Serialize an object to the stream.
        /// </summary>
        /// <param name="source">Object to serialize</param>
        /// <param name="destination">Stream that the serialized version will be written to</param>
        /// <param name="contentType">
        ///     If you include the type name to it after the format name, for instance
        ///     <c>application/protobuf;type=YourApp.DTO.User-YourApp</c>
        /// </param>
        public void Serialize(object source, Stream destination, out byte[] contentType)
        {
            var type = source?.GetType() ?? typeof(object);
            contentType = CreateContentType(type);
            MessagePackSerializer.NonGeneric.Serialize(type, destination, source,
                ContractlessStandardResolver.Instance);
        }

        public void Serialize(object source, Stream destination)
        {
            var type = source?.GetType() ?? typeof(object);
            MessagePackSerializer.NonGeneric.Serialize(type, destination, source,
                ContractlessStandardResolver.Instance);
        }

        /// <summary>
        ///     Returns <c>application/protbuf</c>
        /// </summary>
        public byte[] SupportedContentType => ContentType;

        public ITypeResolver TypeResolver { get; set; }

        /// <summary>
        ///     Deserialize the content from the stream.
        /// </summary>
        /// <returns>
        ///     Created object
        /// </returns>
        /// <exception cref="System.NotSupportedException">Invalid content type</exception>
        public object Deserialize(byte[] contentType, Stream source, out Type resolvedType)
        {
            if (!IsValidContentType(contentType)) throw new NotSupportedException("Invalid decoder");

            var type = Encoding.UTF8.GetString(contentType, 2, contentType.Length - 2);
            if (!Types.TryGetValue(type, out resolvedType)) throw new InvalidCastException("Unknown type");

            return MessagePackSerializer.NonGeneric.Deserialize(resolvedType, source,
                ContractlessStandardResolver.Instance);
        }

        public object DeserializeType(Type type, Stream source)
        {
            return MessagePackSerializer.NonGeneric.Deserialize(type, source,
                ContractlessStandardResolver.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadSerializer Clone()
        {
            return new LegacyMsgPackSerializer() {TypeResolver = this.TypeResolver};
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidContentType(byte[] contentType)
        {
            return contentType?.Length >= 2 && contentType[0] == ContentType[0] && contentType[1] == ContentType[1];
        }

        public byte[] CreateContentType(Type type)
        {
            if (ContentTypeCache.TryGetValue(type, out var b))
                return b;
            var typeS = type.FullName;
            b = new byte[2 + Encoding.UTF8.GetByteCount(typeS)];
            b[0] = ContentType[0];
            b[1] = ContentType[1];
            Encoding.UTF8.GetBytes(typeS, 0, typeS.Length, b, 2);
            lock (ContentTypeCache)
            {
                ContentTypeCache[type] = b;
            }
            return b;
        }

        public void RegisterAssembly(Assembly assembly)
        {
            lock (Types)
            {
                var types = assembly.GetTypes()
                    .Where(t =>
                    {
                        var ti = t.GetTypeInfo();
                        return ti.GetCustomAttribute<MessagePackObjectAttribute>() != null ||
                               ti.GetCustomAttribute<ProtoContractAttribute>() != null;
                    });
                foreach (var type in types)
                    Types[type.FullName] = type;
            }
        }
    }
}
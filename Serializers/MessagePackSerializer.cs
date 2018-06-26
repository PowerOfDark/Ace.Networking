using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.ProtoBuf;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Serializers
{
    public class MsgPackSerializer : IPayloadSerializer
    {
        private static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();

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
            var type = source.GetType();
            contentType = CreateContentType(type);
            if (!Types.ContainsKey(type.FullName))
                MessagePackSerializer.NonGeneric.Serialize(type, destination, source,
                    MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            else
                MessagePackSerializer.NonGeneric.Serialize(type, destination, source);
        }

        public void Serialize(object source, Stream destination)
        {
            var type = source.GetType();
            if (!Types.ContainsKey(type.FullName))
                MessagePackSerializer.NonGeneric.Serialize(type, destination, source,
                    MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            else
                MessagePackSerializer.NonGeneric.Serialize(type, destination, source);
        }

        /// <summary>
        ///     Returns <c>application/protbuf</c>
        /// </summary>
        public byte[] SupportedContentType => ContentType;

        /// <summary>
        ///     Deserialize the content from the stream.
        /// </summary>
        /// <returns>
        ///     Created object
        /// </returns>
        /// <exception cref="System.NotSupportedException">Invalid content type</exception>
        public object Deserialize(byte[] contentType, Stream source, out Type resolvedType)
        {
            if (!IsValidContentType(contentType))
            {
                throw new NotSupportedException("Invalid decoder");
            }

            var type = Encoding.UTF8.GetString(contentType, 2, contentType.Length - 2);
            if (!Types.TryGetValue(type, out resolvedType))
            {
                throw new InvalidCastException("Unknown type");
            }

            return Serializer.NonGeneric.Deserialize(resolvedType, source);
        }

        public object DeserializeType(Type type, Stream source)
        {
            if (!Types.ContainsKey(type.FullName))
                return MessagePackSerializer.NonGeneric.Deserialize(type, source,
                    MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            else
                return MessagePackSerializer.NonGeneric.Serialize(type, source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadSerializer Clone()
        {
            return new MsgPackSerializer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidContentType(byte[] contentType)
        {
            return contentType?.Length >= 2 && contentType[0] == ContentType[0] && contentType[1] == ContentType[1];
        }

        public byte[] CreateContentType(Type type)
        {
            var typeS = type.FullName;
            var b = new byte[2 + Encoding.UTF8.GetByteCount(typeS)];
            b[0] = ContentType[0];
            b[1] = ContentType[1];
            Encoding.UTF8.GetBytes(typeS, 0, typeS.Length, b, 2);
            return b;
        }

        public void RegisterAssembly(Assembly assembly)
        {
            lock (Types)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.GetTypeInfo().GetCustomAttribute(typeof(MessagePackObjectAttribute)) != null);
                foreach (var type in types)
                {
                    Types.Add(type.FullName, type);
                }
            }

        }
    }
}

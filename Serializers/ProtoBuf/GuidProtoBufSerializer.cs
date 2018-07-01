using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ace.Networking.MicroProtocol.Interfaces;
using ProtoBuf;

namespace Ace.Networking.ProtoBuf
{
    /// <summary>
    ///     ProtoBuf serializer that uses <see cref="Guid" />s to define content types.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Guid of a Type is calculated at compile-time,
    ///         and depends on the PublicKeyToken of the containing Assembly
    ///     </para>
    ///     <para>It is recommended to overwrite <see cref="Guid" />s manually, by using <see cref="GuidAttribute" />s.</para>
    /// </remarks>
    public class GuidProtoBufSerializer : IPayloadSerializer
    {
        private static readonly byte[] ContentType = {0x13, 0x37};

        private static readonly ConcurrentDictionary<Guid, Type> Types = new ConcurrentDictionary<Guid, Type>();
        private static readonly ConcurrentDictionary<Type, Guid> TypesLookup = new ConcurrentDictionary<Type, Guid>();
        public byte[] SupportedContentType => ContentType;


        public object Deserialize(byte[] contentType, Stream source, out Type resolvedType)
        {
            if (!IsValidContentType(contentType)) throw new NotSupportedException("Invalid decoder");
            var guidBytes = new byte[16];
            Buffer.BlockCopy(contentType, ContentType.Length, guidBytes, 0, 16);
            var guid = new Guid(guidBytes);
            resolvedType = Types[guid];

            return Serializer.NonGeneric.Deserialize(resolvedType, source);
        }

        public object DeserializeType(Type type, Stream source)
        {
            return Serializer.NonGeneric.Deserialize(type, source);
        }

        public void Serialize(object source, Stream destination, out byte[] contentType)
        {
            var type = source.GetType();

            contentType = CreateContentType(type);
            Serializer.NonGeneric.Serialize(destination, source);
        }

        public void Serialize(object source, Stream destination)
        {
            var type = source.GetType();
            Serializer.NonGeneric.Serialize(destination, source);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidContentType(byte[] contentType)
        {
            return contentType?.Length >= 2 && contentType[0] == ContentType[0] && contentType[1] == ContentType[1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadSerializer Clone()
        {
            return new GuidProtoBufSerializer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] CreateContentType(Type type)
        {
            return CreateContentTypeStatic(type);
        }

        public void RegisterAssembly(Assembly assembly)
        {
            var protoAttribute = typeof(ProtoContractAttribute);
            var types = assembly.GetTypes().Where(t => t.GetTypeInfo().GetCustomAttribute(protoAttribute) != null);
            foreach (var type in types) RegisterType(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterType(Type type)
        {
            var guid = type.GetTypeInfo().GUID;
            Types.TryAdd(guid, type);
            if (!TypesLookup.TryAdd(type, guid))
            {
                //TODO: handle collisions (?)
            }
        }

        public static byte[] CreateContentTypeStatic(Type type)
        {
            var b = new byte[ContentType.Length + 16];
            b[0] = ContentType[0];
            b[1] = ContentType[1];
            var g = TypesLookup[type].ToByteArray();
            Buffer.BlockCopy(g, 0, b, ContentType.Length, 16);

            return b;
        }
    }
}
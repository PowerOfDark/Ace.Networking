using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Structures;

namespace Ace.Networking.Serializers.TypeResolvers
{
    public class GuidTypeResolver : TypeResolverBase<ByteArrayKey>
    {
        public override byte Signature => 0x37;

        public override ByteArrayKey GetRepresentation(Type type)
        {
            return new ByteArrayKey(GetRepresentationUtil(type));
        }

        public byte[] GetRepresentationUtil(Type type, int depth = 1)
        {
            var guid = type.GUID.ToByteArray();
            byte h = 0x4F;
            if (type.IsArray)
            {
                h = 0xF4;
                guid = GetRepresentationUtil(type.BaseType, depth+1);
            }

           
            for (int i = 0; i < 16; i++) guid[i] ^= (h = (byte) ((31 * h) + (depth* depth^0b0001100101)));
            if (type.IsGenericType)
            {
                int m = 1;
                foreach (var g in type.GenericTypeArguments)
                {
                    var tmp = GetRepresentation(g);
                    for (int i = 0; i < 16; i++)
                        guid[i] ^= (h = (byte) ((37 * h) + (m ^ 0b101100101011)));
                    m++;
                }
            }

            return guid;
        }

        public override bool TryWrite(Stream stream, Type type)
        {
            if (!stream.CanWrite)
                throw new ArgumentException(nameof(stream));
            if (!TypesLookup.TryGetValue(type, out var guid)) return false;
            stream.WriteByte(Signature);
            stream.Write(guid.Bytes, 0, 16);
            
            return true;
        }


        public override bool TryResolve(Stream stream, out Type type)
        {
            type = null;
            /*var available = stream.Length - stream.Position;
            if (available < 16)
                return false;*/
            if (stream.ReadByte() != Signature) throw new InvalidDataException("Invalid type resolver signature");
            var buf = new byte[16];
            int read = stream.Read(buf, 0, 16);
            if (read != 16) return false;
            return (Types.TryGetValue(new ByteArrayKey(buf), out type));
        }

    }
}

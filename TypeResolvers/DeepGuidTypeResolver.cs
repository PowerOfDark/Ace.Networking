﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.Serializers.TypeResolvers;
using Ace.Networking.Structures;

namespace Ace.Networking.TypeResolvers
{
    public class DeepGuidTypeResolver : TypeResolverBase<ByteArrayKey>
    {
        public override byte Signature => 0x91;

        public override void RegisterType(Type type)
        {
            while (type.IsArray)
                type = type.GetElementType();
            if (type.IsConstructedGenericType)
            {
                foreach (var generic in type.GetGenericArguments())
                    RegisterType(generic);
                type = type.GetGenericTypeDefinition();
            }
            base.RegisterType(type);
        }

        public override ByteArrayKey GetRepresentation(Type type)
        {
            while (type.IsArray)
                type = type.GetElementType();
            return new ByteArrayKey(type.GUID.ToByteArray());
        }

        public bool SerializeComplexRoot(Stream stream, Type type)
        {
            byte arrayRank = 0;
            byte generics = 0;
            while (type.IsArray)
            {
                arrayRank++;
                type = type.GetElementType();
            }



            ByteArrayKey mainGuid;

            Type[] children = null;
            if (type.IsConstructedGenericType)
            {
                if (!TypesLookup.TryGetValue(type.GetGenericTypeDefinition(), out mainGuid))
                    return false;
                children = type.GetGenericArguments();
                generics += checked((byte)children.Length);
            }
            else
            {
                if (!TypesLookup.TryGetValue(type, out mainGuid))
                    return false;
            }


            if (children != null)
            {
                foreach (var child in children)
                    if (!SerializeComplex(null, child))
                        return false;
            }
            stream.WriteByte(arrayRank);
            stream.WriteByte(generics);
            stream.Write(mainGuid.Bytes, 0, mainGuid.Bytes.Length);

            if (children != null)
            {
                foreach (var child in children)
                    if (!SerializeComplex(stream, child))
                        throw new InvalidOperationException();
            }

            return true;
        }

        public bool SerializeComplex(Stream stream, Type type)
        {
            byte arrayRank = 0;
            byte generics = 0;
            while(type.IsArray)
            {
                arrayRank++;
                type = type.GetElementType();
            }

            ByteArrayKey mainGuid;

            Type[] children = null;
            if (type.IsConstructedGenericType)
            {
                if (!TypesLookup.TryGetValue(type.GetGenericTypeDefinition(), out mainGuid))
                    return false;
                children = type.GetGenericArguments();
                generics += checked((byte)children.Length);
            }
            else
            {
                if (!TypesLookup.TryGetValue(type, out mainGuid))
                    return false;
            }
            if (stream != null)
            {
                stream.WriteByte(arrayRank);
                stream.WriteByte(generics);
                stream.Write(mainGuid.Bytes, 0, mainGuid.Bytes.Length);
            }

            if (children != null)
            {
                foreach (var child in children)
                    if (!SerializeComplex(stream, child))
                        return false;
            }

            return true;
        }

        public bool DeserializeComplex(Stream stream, out Type type)
        {
            type = null;
            int arrayRank = stream.ReadByte();
            if (arrayRank == -1) return false;
            int generics = stream.ReadByte();
            if (generics == -1) return false;

            byte[] chunk = new byte[16];
            int read = stream.Read(chunk, 0, chunk.Length);
            if (read != chunk.Length) return false;

            if (!Types.TryGetValue(new ByteArrayKey(chunk), out type))
                return false;

            if (type.IsGenericTypeDefinition)
            {
                Type[] children = new Type[generics];
                for (int i = 0; i < generics; i++)
                {
                    if (!DeserializeComplex(stream, out var child))
                        return false;
                    children[i] = child;
                }
                type = type.MakeGenericType(children);
            }

            while (arrayRank > 0)
            {
                arrayRank--;
                type = type.MakeArrayType();
            }

            return true;
        }

        public override bool TryWrite(Stream stream, Type type)
        {
            if (!stream.CanWrite)
                throw new ArgumentException(nameof(stream));
            
            stream.WriteByte(Signature);

            return SerializeComplexRoot(stream, type);
        }


        public override bool TryResolve(Stream stream, out Type type)
        {
            type = null;
            /*var available = stream.Length - stream.Position;
            if (available < 16)
                return false;*/
            if (stream.ReadByte() != Signature) throw new InvalidDataException("Invalid type resolver signature");
            return DeserializeComplex(stream, out type);
        }
    }
}

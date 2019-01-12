using System;
using System.IO;
using System.Runtime.InteropServices;
using Ace.Networking.Extensions;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Interfaces;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("E2FE3BD2-6978-4CB6-B498-2289F31E1C98")]
    public class ExecuteMethod : ISerializationListener
    {
        internal Type _ReturnType;
        internal object[] Objects;
        internal Type[] Types;
        public Guid Eid { get; set; }
        public string Method { get; set; }
        public byte[] ReturnType { get; set; }

        public byte[][] Arguments { get; set; }

        public void PreSerialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Method)
            {
                var success = false;
                if (_ReturnType != null)
                    using (var mm = MemoryManager.Instance.GetStream())
                    {
                        success = serializer.TypeResolver.TryWrite(mm, _ReturnType);
                        if (success)
                            ReturnType = mm.ToArray();
                    }

                if (!success)
                    ReturnType = new byte[0];
            }
        }

        public void PostSerialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Method)
            {
                ReturnType = null;
            }
        }

        public void PostDeserialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Method)
            {
                if (ReturnType == null || ReturnType.Length == 0) _ReturnType = null;
                else
                    using (var mm = new MemoryStream(ReturnType))
                    {
                        if (!serializer.TypeResolver.TryResolve(mm, out _ReturnType))
                            _ReturnType = null;
                    }

                if (Arguments == null || Arguments.Length == 0)
                {
                    Types = null;
                    Objects = null;
                }
                else
                {
                    Types = new Type[Arguments.Length];
                    Objects = new object[Arguments.Length];
                    for (var i = 0; i < Arguments.Length; i++)
                    {
                        var arg = Arguments[i];
                        using (var mm = new MemoryStream(arg))
                        {
                            Objects[i] = serializer.Deserialize(serializer.SupportedContentType, mm, out Types[i]);
                        }
                    }
                }

                Arguments = null;
                ReturnType = null;
            }
        }
    }
}
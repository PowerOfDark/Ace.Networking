using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Ace.Networking.Extensions;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Interfaces;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("3CD56B1F-E5BD-4C34-8EF4-7F4DEA06E2A4")]
    public class SelfPlaceholder
    {
        public static readonly SelfPlaceholder Instance = new SelfPlaceholder();
        public SelfPlaceholder() { }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("1CD56B3F-E6BD-1C34-8EF4-7F4DEA06E2A5")]
    public class NullPlaceholder
    {
        public static readonly NullPlaceholder Instance = new NullPlaceholder();
        public NullPlaceholder() { }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("3CD56B1F-E5BD-4C34-8EF4-7F4DEA06E2E2")]
    public class RaiseEvent : ISerializationListener
    {
        internal object[] Objects;
        internal Type[] Types;
        public Guid Eid { get; set; }
        public string Event { get; set; }

        public byte[][] Arguments { get; set; }

        public void PreSerialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Event)
            {
                if ((Objects?.Length ?? 0) > 0)
                {
                    Arguments = new byte[Objects.Length][];
                    using (var mm = MemoryManager.Instance.GetStream())
                    {
                        int i = 0;
                        foreach (var a in Objects)
                        {
                            mm.SetLength(0);
                            serializer.Serialize(a, mm, out _);
                            Arguments[i++] = mm.ToArray();
                        }
                    }

                }
            }
        }

        public void PostSerialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Event)
            {
                Arguments = null;
            }
        }

        public void PostDeserialize(IPayloadSerializer serializer, Stream stream)
        {
            lock (Event)
            {
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
            }
        }
    }
}

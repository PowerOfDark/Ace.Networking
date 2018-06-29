using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("83924DE9-DE48-4D27-B323-518824C38EE5")]
    public class MethodParameter
    {
        public string FullName { get; set; }
        public byte[] SerializedData { get; set; }

        public MethodParameter() { }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("E2FE3BD2-6978-4CB6-B498-2289F31E1C98")]
    public class ExecuteMethod
    {
        public Guid Eid { get; set; }
        public string Method { get; set; }
        public string ReturnValueFullName { get; set; }
        public MethodParameter[] Arguments { get; set; }

        public ExecuteMethod() { }
    }
}

using System.Runtime.InteropServices;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.MicroProtocol.Interfaces;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("3CE78A8D-802A-468F-B88E-706F64738A8D")]
    public class ExecuteMethodResult : IDynamicPayload
    {
        public RemoteExceptionAdapter ExceptionAdapter { get; set; }

        //public byte[] SerializedData { get; set; }
        [ProtoIgnore]
        public object Data;

        public void Construct(object[] payload)
        {
            Data = payload[1];
        }

        public object[] Deconstruct()
        {
            return new object[2] { this, Data };
        }
    }
}
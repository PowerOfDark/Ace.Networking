using System.Runtime.InteropServices;
using Ace.Networking.Entanglement.Structures;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("3CE78A8D-802A-468F-B88E-706F64738A8D")]
    public class ExecuteMethodResult
    {
        public RemoteExceptionAdapter ExceptionAdapter { get; set; }

        public byte[] SerializedData { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Ace.Networking.Entanglement.Structures;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("3CE78A8D-802A-468F-B88E-706F64738A8D")]
    [MessagePackObject]
    public class ExecuteMethodResult
    {
        [Key(0)]
        public RemoteException Exception { get; set; }
        [Key(1)]
        public object Data { get; set; }


        public ExecuteMethodResult()
        {

        }
    }
}

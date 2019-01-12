using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("856879BD-F23D-45D5-8890-EC2714E7EFF2")]
    public class EntangleRequest
    {
        // Optional 
        public Guid? Eid { get; set; }

        public Guid InterfaceId { get; set; }
    }
}
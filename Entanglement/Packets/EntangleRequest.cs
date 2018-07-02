using System;
using System.Runtime.InteropServices;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [MessagePackObject]
    [Guid("856879BD-F23D-45D5-8890-EC2714E7EFF2")]
    public class EntangleRequest
    {
        // Optional
        [Key(0)] public Guid? Eid { get; set; }

        [Key(1)] public Guid InterfaceId { get; set; }
    }
}
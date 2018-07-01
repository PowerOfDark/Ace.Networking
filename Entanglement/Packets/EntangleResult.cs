using System;
using MessagePack;

namespace Ace.Networking.Entanglement.Packets
{
    [MessagePackObject]
    public class EntangleResult
    {
        [Key(0)] public Guid? Eid { get; set; }
    }
}
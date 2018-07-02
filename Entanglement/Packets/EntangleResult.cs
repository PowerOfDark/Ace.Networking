using System;
using System.Runtime.InteropServices;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [MessagePackObject]
    [Guid("25E44344-0F5F-4706-936E-81D321453CE7")]
    public class EntangleResult
    {
        [Key(0)] public Guid? Eid { get; set; }
    }
}
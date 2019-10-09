using System;
using ProtoBuf;

namespace Ace.Networking.MicroProtocol.Enums
{
    [Flags]
    public enum PacketFlag
    {
        None = 0,
        NoContent = 1,
        IsRequest = 2,
        IsResponse = 4,
        MultiContent = 8
    }
}
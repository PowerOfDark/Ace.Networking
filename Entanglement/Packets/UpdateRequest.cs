using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract]
    public class UpdateRequest
    {
        public Guid Eid { get; set; }
    }
}

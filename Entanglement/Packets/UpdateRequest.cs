using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("665A9237-FC49-452F-A4FD-0E3F1B2343E2")]
    public class UpdateRequest
    {
        public Guid Eid { get; set; }
    }
}

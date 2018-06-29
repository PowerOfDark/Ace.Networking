using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ace.Networking.Entanglement.Packets
{
    public class EntangleRequest
    {
        // Optional
        public Guid? Eid { get; set; }

        public Guid InterfaceId { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;

namespace Ace.Networking.Entanglement.Services
{
    public class InterfaceEntry
    {
        public InterfaceDescriptor InterfaceDescriptor { get; set; }
        public Guid InterfaceId { get; set; }
        public EntanglementAccess Access { get; set; }
        public Type Type { get; set; } // where Type : EntangledHostedObjectBase, IEntangledObject
    }
}

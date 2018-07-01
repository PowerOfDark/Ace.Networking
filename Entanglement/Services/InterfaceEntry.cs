using System;
using Ace.Networking.Entanglement.Reflection;

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
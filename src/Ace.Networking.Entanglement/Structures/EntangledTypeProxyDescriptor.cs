using System;
using Ace.Networking.Entanglement.Reflection;

namespace Ace.Networking.Entanglement.Structures
{
    public struct EntangledTypeProxyDescriptor
    {
        public Type GeneratedType { get; set; }
        public InterfaceDescriptor Interface { get; set; }
    }
}
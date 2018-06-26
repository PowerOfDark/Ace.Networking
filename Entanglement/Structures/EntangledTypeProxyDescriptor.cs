using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Entanglement.Reflection;

namespace Ace.Networking.Entanglement.Structures
{
    public struct EntangledTypeProxyDescriptor
    {
        public Type GeneratedType { get; set; }
        public InterfaceDescriptor Interface { get; set; }
    }
}

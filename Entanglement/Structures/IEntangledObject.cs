using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Ace.Networking.Entanglement.Reflection;

namespace Ace.Networking.Entanglement.Structures
{
    public interface IEntangledObject : INotifyPropertyChanging
    {
        Guid Eid { get; }
        InterfaceDescriptor Descriptor { get; }
    }
}

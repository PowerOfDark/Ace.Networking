using System;
using System.ComponentModel;
using Ace.Networking.Entanglement.Reflection;

namespace Ace.Networking.Entanglement.Structures
{
    public interface IEntangledObject : INotifyPropertyChanged
    {
        Guid Eid { get; }
        InterfaceDescriptor Descriptor { get; }
    }
}
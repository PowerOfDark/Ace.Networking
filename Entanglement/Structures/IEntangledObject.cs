using System;
using System.ComponentModel;
using Ace.Networking.Entanglement.Attributes;
using Ace.Networking.Entanglement.Reflection;

namespace Ace.Networking.Entanglement.Structures
{
    public interface IEntangledObject : INotifyPropertyChanged
    {
        [Ignored] Guid _Eid { get; }
        [Ignored] InterfaceDescriptor _Descriptor { get; }
    }
}
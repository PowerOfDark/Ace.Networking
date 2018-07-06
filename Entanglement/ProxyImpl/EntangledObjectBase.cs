using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledObjectBase : IEntangledObject
    {
        public Guid _Eid { get; set; }
        public InterfaceDescriptor _Descriptor { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
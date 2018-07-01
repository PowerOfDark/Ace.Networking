using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledObjectBase : IEntangledObject
    {
        public Guid Eid { get; set; }
        public InterfaceDescriptor Descriptor { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

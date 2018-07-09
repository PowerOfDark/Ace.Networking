using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Ace.Networking.Entanglement.Services;

namespace Ace.Networking.Entanglement
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EntanglementAttribute : Attribute
    {
        
        private Type _interface;

        public Type Interface
        {
            get => _interface;
            set
            {
                if (value != null)
                {
                    var ti = value.GetTypeInfo();
                    if (!ti.IsInterface || !ti.IsPublic)
                        throw new Exception("The provided type must be a public interface");
                }
                _interface = value;
            }
        }

        public EntanglementAccess Access { get; set; } = EntanglementAccess.Scoped;

        public EntanglementAttribute()
        {
            
        }
    }
}

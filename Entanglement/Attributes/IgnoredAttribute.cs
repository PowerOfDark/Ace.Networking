using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Entanglement.Attributes
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class IgnoredAttribute : Attribute
    {
        public IgnoredAttribute()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Entanglement
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class IgnoredAttribute : Attribute
    {
        public IgnoredAttribute()
        {
        }
    }
}

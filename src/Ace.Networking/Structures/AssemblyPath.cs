using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ace.Networking.Structures
{
    public struct AssemblyPath
    {
        public string Namespace;
        public Assembly Assembly;

        public AssemblyPath(Assembly assembly, string @namespace = "")
        {
            Namespace = @namespace;
            Assembly = assembly;
        }
    }
}

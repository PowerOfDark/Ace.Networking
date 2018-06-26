using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Ace.Networking.Entanglement.Reflection
{
    //Provides a generic dynamic assembly
    public static class DynamicAssembly
    {
        private static AssemblyBuilder _assembly = null;

        public static AssemblyBuilder Assembly
        {
            get
            {
                if (_assembly == null)
                {
                    lock (_assembly)
                    {
                        _assembly = AssemblyBuilder.DefineDynamicAssembly(
                            new AssemblyName($"Ace_{nameof(DynamicAssembly)}"),
                            AssemblyBuilderAccess.RunAndCollect);
                    }
                }

                return _assembly;

            }
        }
    }
}

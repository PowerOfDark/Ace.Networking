using System.Reflection;
using System.Reflection.Emit;

namespace Ace.Networking.Entanglement.Reflection
{
    //Provides a generic dynamic assembly
    public static class DynamicAssembly
    {
        private static readonly object _sync = new object();
        private static AssemblyBuilder _assembly;

        public static AssemblyBuilder Assembly
        {
            get
            {
                lock (_sync)
                {
                    if (_assembly == null)
                        _assembly = AssemblyBuilder.DefineDynamicAssembly(
                            new AssemblyName($"Ace_{nameof(DynamicAssembly)}"),
                            AssemblyBuilderAccess.RunAndCollect);
                }

                return _assembly;
            }
        }
    }
}
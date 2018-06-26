using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Structures;

namespace Ace.Networking.Entanglement.Reflection
{
    public class EntanglementLocalProxyProvider
    {
        private static ModuleBuilder _dynamicModule = null;

        public static ModuleBuilder DynamicModule
        {
            get
            {
                if (_dynamicModule == null)
                {
                    lock (_dynamicModule)
                    {
                        var assembly = AssemblyBuilder.DefineDynamicAssembly(
                            new AssemblyName($"{nameof(EntanglementLocalProxyProvider)}"),
                            AssemblyBuilderAccess.RunAndCollect);
                        _dynamicModule = assembly.DefineDynamicModule(Guid.NewGuid().ToString());
                    }
                }

                return _dynamicModule;

            }
        }


        public static ConcurrentDictionary<Guid, EntangledTypeProxyDescriptor> GeneratedTypes = new ConcurrentDictionary<Guid, EntangledTypeProxyDescriptor>();

        public static T Get<T>(Guid eid) where T : IEntangledObject
        {
            EntangledTypeProxyDescriptor desc;
            var guid = typeof(T).GetTypeInfo().GUID;
            if (!GeneratedTypes.TryGetValue(guid, out desc))
            {
                desc = ConstructLocalProxy<T>();
                GeneratedTypes.TryAdd(guid, desc);
            }
         

            T obj = (T)Activator.CreateInstance(desc.GeneratedType);
            obj.Eid = eid;
            obj.Descriptor = desc.Interface;
            return obj;
        }

        private static EntangledTypeProxyDescriptor ConstructLocalProxy<T>() where T: IEntangledObject
        {
            var desc = new InterfaceDescriptor(typeof(T));
            var guid = typeof(T).GetTypeInfo().GUID;
            var type = DynamicModule.DefineType($"T{guid.ToString()}", TypeAttributes.Public, typeof(EntangledLocalObjectBase));
            type.AddInterfaceImplementation(typeof(T));


            return new EntangledTypeProxyDescriptor() {GeneratedType = type.AsType(), Interface = desc};
            //var type = DynamicModule.DefineType()
        }

    }
}

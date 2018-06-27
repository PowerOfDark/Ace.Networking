using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Ace.Networking.Entanglement.Extensions;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.Reflection
{
    public class EntanglementLocalProxyProvider
    {
        private static object _sync = new object();
        private static ModuleBuilder _dynamicModule = null;

        public static ModuleBuilder DynamicModule
        {
            get
            {
                lock (_sync)
                {
                    if (_dynamicModule == null)
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

        public static T Get<T>(IConnection host, Guid eid) where T : IEntangledObject
        {
            EntangledTypeProxyDescriptor desc;
            var guid = typeof(T).GetTypeInfo().GUID;
            if (!GeneratedTypes.TryGetValue(guid, out desc))
            {
                desc = ConstructLocalProxy<T>();
                GeneratedTypes.TryAdd(guid, desc);
            }
         
           
            T obj = (T)Activator.CreateInstance(desc.GeneratedType, host, eid, desc.Interface);
            return obj;
        }

        private static EntangledTypeProxyDescriptor ConstructLocalProxy<T>() where T: IEntangledObject
        {
            var typeInfo = typeof(T).GetTypeInfo();
            if (!typeInfo.IsInterface)
                throw new ArgumentException("The provided type must be an interface");

            var desc = new InterfaceDescriptor(typeof(T));
            var guid = typeInfo.GUID;
            var type = DynamicModule.DefineType($"T{guid.ToString()}", TypeAttributes.Class | TypeAttributes.Public, typeof(EntangledLocalObjectBase));
            type.AddInterfaceImplementation(typeof(T));

            var executeMethod =
                typeof(EntangledLocalObjectBase).GetMethod("ExecuteMethod", BindingFlags.Public | BindingFlags.Instance);

            var executeMethodVoid =
                typeof(EntangledLocalObjectBase).GetMethod("ExecuteMethodVoid",
                    BindingFlags.Public | BindingFlags.Instance);

            type.FillBaseConstructors(typeof(EntangledLocalObjectBase));

            foreach (var methods in desc.Methods)
            {
                foreach (var method in methods.Value)
                {
                    var parameters = method.Method.GetParameters();
                    var m = type.DefineMethod(method.Method.Name, MethodAttributes.Public | MethodAttributes.Virtual, method.Method.CallingConvention, method.Method.ReturnType, method.Method.GetParameters().Select(p=>p.ParameterType).ToArray());
                    var i = m.GetILGenerator();

                    i.Emit(OpCodes.Ldarg_0);
                    i.Emit(OpCodes.Ldstr, method.Method.Name);

                    //emit the object[] array to hold the parameters

                    if ((parameters?.Length ?? 0) == 0)
                    {
                        i.Emit(OpCodes.Ldnull);
                    }
                    else
                    {
                        i.EmitLdci4((byte)parameters.Length);
                        i.Emit(OpCodes.Newarr, typeof(object));
                        for (byte l = 0; l < parameters.Length; l++)
                        {
                            i.Emit(OpCodes.Dup);
                            i.EmitLdci4(l);
                            i.EmitLdarg((byte)(l + 1));
                            if (parameters[l].ParameterType.GetTypeInfo().IsValueType)
                            {
                                i.Emit(OpCodes.Box, parameters[l].ParameterType);
                            }

                            i.Emit(OpCodes.Stelem_Ref);
                        }
                    }

                    if (method.RealReturnType == typeof(void))
                    {
                        i.Emit(OpCodes.Call, executeMethodVoid);
                    }
                    else
                    {
                        i.Emit(OpCodes.Call, executeMethod.MakeGenericMethod(method.RealReturnType));
                    }

                    i.Emit(OpCodes.Ret);
                    
                    //important: OVERRIDE THE INTERFACE [abstract] IMPLEMENTATION
                    type.DefineMethodOverride(m, method.Method);
                }
            }

            
            return new EntangledTypeProxyDescriptor() {GeneratedType = type.CreateTypeInfo().AsType(), Interface = desc};
        }

    }
}

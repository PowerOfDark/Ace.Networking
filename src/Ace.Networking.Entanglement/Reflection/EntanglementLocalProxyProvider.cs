using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ace.Networking.Entanglement.Extensions;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Threading;

namespace Ace.Networking.Entanglement.Reflection
{
    public class EntanglementLocalProxyProvider
    {
        public static ConcurrentDictionary<Guid, EntangledTypeProxyDescriptor> GeneratedTypes =
            new ConcurrentDictionary<Guid, EntangledTypeProxyDescriptor>();

        public static EntangledLocalObjectBase Get<T>(IConnection host, Guid eid) where T : class/*, IEntangledObject*/
        {
            EntangledTypeProxyDescriptor desc;
            var guid = typeof(T).GetTypeInfo().GUID;
            lock (GeneratedTypes)
            {
                if (!GeneratedTypes.TryGetValue(guid, out desc))
                {
                    desc = ConstructLocalProxy<T>();
                    GeneratedTypes.TryAdd(guid, desc);
                }
            }


            var obj = (EntangledLocalObjectBase)Activator.CreateInstance(desc.GeneratedType, host, eid,
                desc.Interface);
            return obj;
        }

        private static EntangledTypeProxyDescriptor ConstructLocalProxy<T>() where T : class/*, IEntangledObject*/
        {
            var typeInfo = typeof(T).GetTypeInfo();
            if (!typeInfo.IsInterface || !typeInfo.IsPublic)
                throw new ArgumentException("The provided type must be a public interface");

            var elo = typeof(EntangledLocalObjectBase);

            var desc = new InterfaceDescriptor(typeof(T));
            var guid = typeInfo.GUID;
            var type = DynamicAssembly.DynamicModule.DefineType($"T{guid.ToString()}", TypeAttributes.Class | TypeAttributes.Public,
                elo);
            type.AddInterfaceImplementation(typeof(T));

            var executeMethod =
                elo.GetMethod("ExecuteMethod",
                    BindingFlags.Public | BindingFlags.Instance);

            var executeMethodVoid =
                elo.GetMethod("ExecuteMethodVoid",
                    BindingFlags.Public | BindingFlags.Instance);

            var executeMethodSync = elo.GetMethod("ExecuteMethodSync", BindingFlags.Public | BindingFlags.Instance);
            var executeMethodVoidSync =
                elo.GetMethod("ExecuteMethodVoidSync", BindingFlags.Public | BindingFlags.Instance);


            type.FillBaseConstructors(elo);

            foreach (var methods in desc.Methods)
                foreach (var method in methods.Value)
                {
                    var parameters = method.Method.GetParameters();
                    var m = type.DefineMethod(method.Method.Name, MethodAttributes.Public | MethodAttributes.Virtual,
                        method.Method.CallingConvention, method.Method.ReturnType,
                        method.Method.GetParameters().Select(p => p.ParameterType).ToArray());
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
                                i.Emit(OpCodes.Box, parameters[l].ParameterType);

                            i.Emit(OpCodes.Stelem_Ref);
                        }
                    }

                    if (method.RealReturnType == typeof(void))
                        i.Emit(OpCodes.Call, method.IsAsync ? executeMethodVoid : executeMethodVoidSync);
                    else
                        i.Emit(OpCodes.Call, (method.IsAsync ? executeMethod : executeMethodSync).MakeGenericMethod(method.RealReturnType));

                    i.Emit(OpCodes.Ret);

                    //important: OVERRIDE THE INTERFACE [abstract] IMPLEMENTATION
                    type.DefineMethodOverride(m, method.Method);
                }

            //implement getters

            var generatedProperties = new Queue<string>();

            foreach (var propItem in desc.Properties)
            {
                var prop = propItem.Value.Property;
                var baseProp = typeof(EntangledLocalObjectBase).GetProperty(propItem.Key,
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                if (baseProp != null && !baseProp.GetGetMethod().IsAbstract)
                    continue;

                var definedGetter = prop.GetGetMethod();

                var field = type.DefineField($"_{prop.Name}", prop.PropertyType, FieldAttributes.Private);
                propItem.Value.BackingField = field;
                var getProp = type.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, null);
                var getter = type.DefineMethod($"get_{prop.Name}",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                    definedGetter.ReturnType, null);

                var il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);

                getProp.SetGetMethod(getter);

                type.DefineMethodOverride(getter, definedGetter);

                generatedProperties.Enqueue(propItem.Key);
            }

            foreach (var ev in desc.Events)
            {
                type.ImplementEvent(ev.Value.Event.DeclaringType.GetTypeInfo(), ev.Key);
            }


            var result =
                new EntangledTypeProxyDescriptor { GeneratedType = type.CreateTypeInfo().AsType(), Interface = desc };

            foreach (var ev in desc.Events)
            {
                ev.Value.BackingField =
                    result.GeneratedType.GetField(ev.Key, BindingFlags.NonPublic | BindingFlags.Instance);
                ev.Value.InvokerDelegate = CreateEventInvokerDelegate(type, ev.Value, typeof(T));
            }
            while (generatedProperties.Any())
            {
                var prop = generatedProperties.Dequeue();
                desc.Properties[prop].BackingField = result.GeneratedType.GetField($"_{prop}",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }

            return result;
        }

        private static Action<object, object[]> CreateEventInvokerDelegate(TypeBuilder b, EventDescriptor ev, Type target)
        {
            var args = ev.InvokeMethod.GetParameters();
            var dm = new DynamicMethod($"I{ev.InvokeMethod.Name}", ev.InvokeMethod.ReturnType == typeof(void) ? null : typeof(object), DelegateHelper.DelegateParameters, DynamicAssembly.DynamicModule, true);
            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, target);
            il.Emit(OpCodes.Ldfld, ev.BackingField);
            il.Emit(OpCodes.Dup);
            var mRet = il.DefineLabel();
            var mOk = il.DefineLabel();

            il.Emit(OpCodes.Brtrue_S, mOk);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Br_S, mRet);
            il.MarkLabel(mOk);


            for (int i = 0; i < args.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitLdci4(i);
                il.Emit(OpCodes.Ldelem_Ref);
                var pt = args[i].ParameterType;
                if (pt.GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, pt);
                }
                else il.Emit(OpCodes.Castclass, pt);
            }
            il.Emit(OpCodes.Call, ev.InvokeMethod);
            il.MarkLabel(mRet);
            il.Emit(OpCodes.Ret);
            return (Action<object, object[]>)dm.CreateDelegate(typeof(Action<object, object[]>));
        }
    }
}
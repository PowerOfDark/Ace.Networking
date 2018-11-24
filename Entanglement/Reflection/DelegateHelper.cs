using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Ace.Networking.Entanglement.Extensions;

namespace Ace.Networking.Entanglement.Reflection
{
    public static class DelegateHelper
    {
        public static readonly Type[] DelegateParameters = {typeof(object), typeof(object[])};

        public static Delegate ConstructDelegateCall(MethodInfo method, Type target)
        {
            var args = method.GetParameters();
            var dm = new DynamicMethod($"D{method.Name}", method.ReturnType == typeof(void) ? null : typeof(object), DelegateParameters, true);
            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, target);

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

            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Ret);

            var funcType = method.ReturnType == typeof(void)
                ? typeof(Action<object, object[]>)
                : typeof(Func<object, object[], object>);

            return dm.CreateDelegate(funcType);
        }

        public static Action<object, object[]> ConstructDelegateCallVoid(MethodInfo method, Type target)
        {
            return (Action<object, object[]>) ConstructDelegateCall(method, target);
        }

        public static Func<object, object[], object> ConstructDelegateCallFunc(MethodInfo method, Type target)
        {
            return (Func<object, object[], object>) ConstructDelegateCall(method, target);
        }

    }
}

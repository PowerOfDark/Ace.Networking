using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class TypeBuilderExtensions
    {
        public static void FillBaseConstructors(this TypeBuilder t, Type baseType, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
        {
            var ctors = baseType.GetConstructors(flags);
            foreach (var ctor in ctors)
            {
                var par = ctor.GetParameters();
                var c = t.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                    par?.Select(p => p.ParameterType).ToArray() ?? null);
                var il = c.GetILGenerator();

                for (byte ld = 0; ld < (par?.Length??0)+1; ld++)
                {
                    il.EmitLdarg(ld);
                }

                il.Emit(OpCodes.Call, ctor);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ret);
            }
        }
        public static void EmitLdarg(this ILGenerator il, byte i)
        {
            
            switch (i)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldarg_S, i);
                    return;
            }
        }

        public static void EmitLdci4(this ILGenerator il, byte i)
        {
            
            switch (i)
            {
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    il.Emit(OpCodes.Ldc_I4_S, i);
                    return;
            }
        }
    }


}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;

namespace Ace.Networking.Entanglement.Reflection
{
    public struct ParameterDescriptor
    {
        public bool IsOptional;
        public Type Type;
    }
    public class MethodDescriptor
    {
        public MethodInfo Method;
        public Type RealReturnType;
        public ParameterDescriptor[] Parameters;
    }

    public class InterfaceDescriptor
    {
        public Type Type { get; }
        public Dictionary<string, PropertyInfo> Properties = new Dictionary<string, PropertyInfo>();
        public Dictionary<string, LinkedList<MethodDescriptor>> Methods = new Dictionary<string, LinkedList<MethodDescriptor>>();

        public static ConcurrentDictionary<Type, InterfaceDescriptor> Cache =
            new ConcurrentDictionary<Type, InterfaceDescriptor>();

        public static InterfaceDescriptor Get(Type type)
        {
            if (!Cache.TryGetValue(type, out var desc))
            {
                Cache.TryAdd(type, desc = new InterfaceDescriptor(type));
            }
            return desc;
        }

        public InterfaceDescriptor(Type t, bool onlyVirtualProperties = false)
        {
            Type = t;
            Construct(t, onlyVirtualProperties);
        }

        private void Construct(Type t, bool onlyVirtualProperties = false)
        {
            FillProperties(t, onlyVirtualProperties);
            FillMethods(t);
        }

        private void FillProperties(Type t, bool onlyVirtual = false)
        {
            if (t.GetTypeInfo().IsInterface)
            {
                var q = new Queue<Type>();
                var seen = new HashSet<Type>();
                q.Enqueue(t);
                while (q.Any())
                {
                    var current = q.Dequeue();
                    foreach (var p in current.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        Properties[p.Name] = p;
                    }
                    foreach (var i in t.GetInterfaces())
                    {
                        if (seen.Contains(i)) continue;
                        q.Enqueue(i);
                        seen.Add(current);

                    }

                }
                return;
            }

            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var p in properties)
            {
                if (onlyVirtual)
                {
                    try
                    {
                        if (!p.GetGetMethod().IsVirtual) continue;
                    }
                    catch
                    {
                    }
                }

                Properties[p.Name] = p;
            }
        }

        public static Type UnwrapTask(Type task)
        {
            if (task == typeof(Task)) return typeof(void);
            var info = task.GetTypeInfo();
            if (info.IsGenericType && info.BaseType == typeof(Task))
                return info.GenericTypeArguments.FirstOrDefault();
            return null;
        }

        private void FillMethods(Type t)
        {
            bool isInterface = t.GetTypeInfo().IsInterface;
            void fillMethod(MethodInfo m)
            {
                if (m.IsGenericMethod || m.IsSpecialName) return;
                var realRet = UnwrapTask(m.ReturnType);
                if (realRet == null)
                {
                    if (isInterface)
                        throw new InvalidCastException(
                            "The entangled interface methods need to return either Task or Task<T>.");
                    else return;
                }

                Methods.Append(m.Name, new MethodDescriptor()
                {
                    Method = m,
                    Parameters =
                        m.GetParameters().Select(p => new ParameterDescriptor() { IsOptional = p.IsOptional, Type = p.ParameterType }).ToArray(),
                    RealReturnType = realRet
                    
                });
            }
            var flags = BindingFlags.Instance | BindingFlags.Public;
            if (isInterface)
            {
                var q = new Queue<Type>();
                var seen = new HashSet<Type>();
                q.Enqueue(t);
                while (q.Any())
                {
                    var current = q.Dequeue();
                    foreach (var m in current.GetMethods(flags))
                    {
                        fillMethod(m);
                    }
                    foreach (var i in t.GetInterfaces())
                    {
                        if (seen.Contains(i)) continue;
                        q.Enqueue(i);
                        seen.Add(current);

                    }

                }
                return;
            }

            var methods = t.GetMethods(flags);
            foreach (var method in methods)
            {
                fillMethod(method);
            }
        }

        public MethodDescriptor FindOverload(ExecuteMethod cmd)
        {
            foreach (var m in Methods[cmd.Method])
            {
                if (m.RealReturnType.FullName == cmd.ReturnValueFullName && m.Parameters.Length == (cmd.Arguments?.Length??0))
                {
                    int i = 0;
                    bool err = false;
                    for (; i < m.Parameters.Length && i < cmd.Arguments?.Length; i++)
                    {
                        if (m.Parameters[i].Type.FullName != cmd.Arguments[i].FullName)
                        {
                            err = true;
                            break;
                        }
                    }

                    if (err) continue;
                    return m;
                }
            }
            return null;
        }
    }
}

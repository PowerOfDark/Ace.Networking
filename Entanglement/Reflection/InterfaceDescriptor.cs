using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Attributes;
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
        public ParameterDescriptor[] Parameters;
        public Type RealReturnType;
    }

    public class PropertyDescriptor : IEqualityComparer<PropertyDescriptor>
    {
        public FieldInfo BackingField;
        public PropertyInfo Property;

        public bool Equals(PropertyDescriptor x, PropertyDescriptor y)
        {
            return Equals(x?.Property, y?.Property);
        }

        public int GetHashCode(PropertyDescriptor obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }

    public class InterfaceDescriptor
    {
        public static ConcurrentDictionary<Type, InterfaceDescriptor> Cache =
            new ConcurrentDictionary<Type, InterfaceDescriptor>();

        private readonly Dictionary<string, IReadOnlyCollection<MethodDescriptor>> _methods =
            new Dictionary<string, IReadOnlyCollection<MethodDescriptor>>();

        private readonly Dictionary<string, PropertyDescriptor> _properties =
            new Dictionary<string, PropertyDescriptor>();

        public InterfaceDescriptor(Type t, bool onlyVirtualProperties = false)
        {
            Type = t;
            Construct(t, onlyVirtualProperties);
        }

        public Type Type { get; }

        public IReadOnlyDictionary<string, PropertyDescriptor> Properties => _properties;
        public IReadOnlyDictionary<string, IReadOnlyCollection<MethodDescriptor>> Methods => _methods;

        public static InterfaceDescriptor Get(Type type)
        {
            if (!Cache.TryGetValue(type, out var desc)) Cache.TryAdd(type, desc = new InterfaceDescriptor(type));
            return desc;
        }

        private void Construct(Type t, bool onlyVirtualProperties = false)
        {
            var info = t.GetTypeInfo();
            if (!info.IsInterface || !info.IsPublic)
                throw new ArgumentException("The provided type must be a public interface");

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
                        if (p.GetCustomAttribute<IgnoredAttribute>() != null) continue;
                        _properties[p.Name] = new PropertyDescriptor {Property = p};
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
                if (p.GetCustomAttribute<IgnoredAttribute>() != null) continue;
                if (onlyVirtual)
                    try
                    {
                        if (!p.GetGetMethod().IsVirtual) continue;
                    }
                    catch
                    {
                    }

                _properties[p.Name] = new PropertyDescriptor {Property = p};
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
            var isInterface = t.GetTypeInfo().IsInterface;

            void fillMethod(MethodInfo m)
            {
                if (m.IsGenericMethod || m.IsSpecialName || m.GetCustomAttribute<IgnoredAttribute>() != null) return;
                var realRet = UnwrapTask(m.ReturnType);
                if (realRet == null)
                {
                    if (isInterface)
                        throw new InvalidCastException(
                            "The entangled interface methods need to return either Task or Task<T>.");
                    return;
                }

                LinkedList<MethodDescriptor> list;

                if (!_methods.TryGetValue(m.Name, out var c))
                    _methods[m.Name] = list = new LinkedList<MethodDescriptor>();
                else
                    list = (LinkedList<MethodDescriptor>) c;


                list.AddLast(new MethodDescriptor
                {
                    Method = m,
                    Parameters =
                        m.GetParameters().Select(p =>
                            new ParameterDescriptor {IsOptional = p.IsOptional, Type = p.ParameterType}).ToArray(),
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
                    foreach (var m in current.GetMethods(flags)) fillMethod(m);
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
            foreach (var method in methods) fillMethod(method);
        }

        public MethodDescriptor FindOverload(ExecuteMethod cmd)
        {
            foreach (var m in _methods[cmd.Method])
                if (m.RealReturnType.FullName == cmd.ReturnValueFullName &&
                    m.Parameters.Length == (cmd.Arguments?.Length ?? 0))
                {
                    var i = 0;
                    var err = false;
                    for (; i < m.Parameters.Length && i < cmd.Arguments?.Length; i++)
                        if (m.Parameters[i].Type.FullName != cmd.Arguments[i].FullName)
                        {
                            err = true;
                            break;
                        }

                    if (err) continue;
                    return m;
                }

            return null;
        }
    }
}
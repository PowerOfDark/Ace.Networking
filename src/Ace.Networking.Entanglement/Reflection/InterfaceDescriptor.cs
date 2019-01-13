using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Extensions;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.ProxyImpl;

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
        public bool IsAsync;

        public Func<object, object[], object> InvokerDelegate;

    }

    public class EventDescriptor
    {
        public FieldInfo BackingField;
        public EventInfo Event;
        public ParameterDescriptor[] Parameters;
        public MethodInfo InvokeMethod;

        public DynamicMethod HandlerDelegate;
        public Action<object, object[]> InvokerDelegate;
        
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

        private readonly Dictionary<string, EventDescriptor> _events = new Dictionary<string, EventDescriptor>();

        public InterfaceDescriptor(Type t, bool onlyVirtualProperties = false)
        {
            Type = t;
            Construct(t, onlyVirtualProperties);
        }

        public Type Type { get; }

        public IReadOnlyDictionary<string, PropertyDescriptor> Properties => _properties;
        public IReadOnlyDictionary<string, IReadOnlyCollection<MethodDescriptor>> Methods => _methods;
        public IReadOnlyDictionary<string, EventDescriptor> Events => _events;

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
            FillEvents(t);
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

                /*if (realRet == null)
                {
                    if (isInterface)
                        throw new InvalidCastException(
                            "The entangled interface methods need to return either Task or Task<T>.");
                    return;
                }*/

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
                    RealReturnType = realRet ?? m.ReturnType,
                    IsAsync = realRet != null,
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

        private void FillEvents(Type t)
        {
            var events = t.GetEvents();

            foreach(var ev in events)
            {
                if (ev.GetCustomAttribute<IgnoredAttribute>() != null) continue;
                EventDescriptor desc;
                _events.Add(ev.Name, desc = new EventDescriptor()
                {
                    Event = ev,
                    InvokeMethod = ev.EventHandlerType?.GetMethod("Invoke"),
                });
                desc.Parameters = desc.InvokeMethod?.GetParameters().Select(p =>
                    new ParameterDescriptor() {IsOptional = p.IsOptional, Type = p.ParameterType}).ToArray();
            }

        }

        public int Test(EntangledHostedObjectBase ba, int a, string b, DateTime c)
        {
            ba.OnEvent("shit", new object[] {a, b, c,});
            return 5 + 7;
        }

        public int DoSth(object obj, object[] args)
        {
            return ((InterfaceDescriptor) obj).Test((EntangledHostedObjectBase)args[0], (int)args[1], (string)args[2], (DateTime)args[3]);
        }


        public void AddEventInvokerDelegate(EventDescriptor ev)
        {
            ev.InvokerDelegate = DelegateHelper.ConstructDelegateCallVoid(ev.InvokeMethod, ev.Event.EventHandlerType);
        }

        public void AddEventHandlerDelegate(EventDescriptor ev, Type handler)

        {
            Type[] args = new Type[ev.Parameters.Length + 1];
            args[0] = handler;
            for (int i = 0; i < ev.Parameters.Length; i++) args[i + 1] = ev.Parameters[i].Type;
            var m = new DynamicMethod($"H{ev.Event.Name}", typeof(void), args, DynamicAssembly.DynamicModule, true);
            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, ev.Event.Name);


            if ((ev.Parameters?.Length ?? 0) == 0)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                il.EmitLdci4((byte)ev.Parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(object));
                for (byte l = 0; l < ev.Parameters.Length; l++)
                {
                    il.Emit(OpCodes.Dup);
                    il.EmitLdci4(l);
                    il.EmitLdarg((byte)(l + 1));
                    if (ev.Parameters[l].Type.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, ev.Parameters[l].Type);

                    il.Emit(OpCodes.Stelem_Ref);
                }
            }



            il.Emit(OpCodes.Call, handler.GetMethod("OnEvent", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            //var d = m.CreateDelegate(ev.Event.EventHandlerType);
            ev.HandlerDelegate = m;
            //il.EmitCall(OpCodes.Call, handler.GetMethod("OnEvent"), )

        }

        public void AddMethodDelegate(MethodDescriptor method)
        {
            method.InvokerDelegate = DelegateHelper.ConstructDelegateCallFunc(method.Method, this.Type);
        }

        public MethodDescriptor FindOverload(ExecuteMethod cmd)
        {
            foreach (var m in _methods[cmd.Method])
                if (/*cmd._ReturnType.IsAssignableFrom(m.RealReturnType) &&*/
                    m.Parameters.Length == (cmd.Objects?.Length ?? 0))
                {
                    var i = 0;
                    var err = false;
                    for (; i < m.Parameters.Length && i < cmd.Objects?.Length; i++)
                    {
                        var type = cmd.Objects[i]?.GetType();
                        if (type != null && !m.Parameters[i].Type.IsAssignableFrom(type))
                        {
                            err = true;
                            break;
                        }
                    }

                    if (err) continue;
                    return m;
                }

            return null;
        }
    }
}
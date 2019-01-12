using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ace.Networking.Helpers
{
    public class DependencyResolver
    {
        public static IDictionary<Type, T> Resolve<T>(IDictionary<Type, (Type type, T instance)> map) where T : class
        {
            //var unmapped = new Queue<Type>();
            var res = new Dictionary<Type, T>(map.Count);
            foreach (var kv in map)
                if (kv.Value.instance != null)
                    res.Add(kv.Key, kv.Value.instance);
            //if (!unmapped.Any()) return;

            void Traverse(Type source)
            {
                var toVisit = new Stack<Type>();
                toVisit.Push(source);
                while (toVisit.Count > 0)
                {
                    var node = toVisit.Peek();
                    if (!map.TryGetValue(node, out var mn))
                    {
                        toVisit.Pop();
                        continue;
                    }

                    if (!res.TryGetValue(node, out var obj))
                    {
                        var children = GetChildren(mn.type).Where(c =>
                        {
                            if (!res.TryGetValue(c, out var r)) return true;
                            if (r == null)
                                throw new Exception(
                                    $"Cyclic dependency: [{mn.type.FullName}]<->[{map[c].type.FullName}]");
                            return false;
                        }).ToList();
                        if (children.Any())
                        {
                            res.Add(node, null);
                            toVisit.PushReverse(children);
                            continue;
                        }
                    }
                    if (obj == null)
                    {
                        var ret = Construct(mn.type, res);
                        res[node] = ret;
                        toVisit.Pop();
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            /*while (unmapped.Any())
            {
                var u = unmapped.Dequeue();
                if (map[u] == null)
                    Traverse(u);
            }*/
            foreach (var kv in map)
                if (!res.ContainsKey(kv.Key))
                    Traverse(kv.Key);
            return res;
        }

        private static IEnumerable<Type> GetChildren(Type t)
        {
            var ctor = t.GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
            return ctor.GetParameters().Select(p => p.ParameterType);
        }

        private static T Construct<T>(Type type, IDictionary<Type, T> map) where T : class
        {
            var ctor = type.GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
            var parameters = ctor.GetParameters();
            var obj = new object[parameters.Length];
            var i = 0;
            foreach (var p in parameters)
                if (!map.TryGetValue(p.ParameterType, out var pType) || pType == null)
                {
                    if (p.IsOptional || p.HasDefaultValue)
                        obj[i++] = p.HasDefaultValue ? p.DefaultValue : null;
                    else
                        throw new NotSupportedException(
                            $"Missing dependency for {type.FullName}: {p.ParameterType.FullName}");
                }
                else
                {
                    obj[i++] = pType;
                }

            return (T) Activator.CreateInstance(type, obj);
        }
    }

    internal static class StackHelper
    {
        public static T PeekOrDefault<T>(this Stack<T> s)
        {
            return s.Count == 0 ? default : s.Peek();
        }

        public static void PushReverse<T>(this Stack<T> s, IEnumerable<T> list)
        {
            foreach (var l in list.Reverse()) s.Push(l);
        }
    }
}
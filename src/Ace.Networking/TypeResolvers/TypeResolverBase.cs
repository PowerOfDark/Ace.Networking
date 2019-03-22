using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProtoBuf;

namespace Ace.Networking.TypeResolvers
{
    public abstract class TypeResolver<TKey> : ITypeResolver
    {
        protected static readonly ConcurrentDictionary<TKey, Type> Types;
        protected static readonly ConcurrentDictionary<Type, TKey> TypesLookup;
        protected static readonly HashSet<Assembly> Assemblies = new HashSet<Assembly>();


        static TypeResolver()
        {
            Types = Types ?? new ConcurrentDictionary<TKey, Type>();
            TypesLookup = TypesLookup ?? new ConcurrentDictionary<Type, TKey>();
        }


        public abstract byte Signature { get; }

        public virtual void RegisterAssembly(Assembly assembly, params Type[] attributes)
        {
            
            foreach (var type in assembly.GetTypes()
                .Where(t => attributes.Any(a => t.GetTypeInfo().GetCustomAttribute(a) != null)))
                RegisterType(type);
        }

        public virtual void RegisterAssembly(Assembly assembly)
        {
            if (!Assemblies.Add(assembly)) return;
            RegisterAssembly(assembly, typeof(ProtoContractAttribute));
        }

        public abstract bool TryResolve(Stream stream, out Type type);

        public abstract bool TryWrite(Stream stream, Type type);

        

        public virtual void RegisterType(Type type, TKey key)
        {
            var guid = key;
            if (Types.TryGetValue(guid, out var t1) && t1 != type /*||
                TypesLookup.TryGetValue(type, out var t2) && !EqualityComparer<TKey>.Default.Equals(t2, guid)*/)
                throw new ArgumentException(nameof(type));
            Types[guid] = type;
            TypesLookup[type] = guid;
        }

        public virtual void RegisterTypeBy(Type type, Guid guid)
        {
            RegisterType(type, GetBy(guid));
        }

        public virtual void RegisterType(Type type)
        {
            var guid = GetRepresentation(type);
            RegisterType(type, guid);
        }

        public abstract TKey GetRepresentation(Type type);
        public abstract TKey GetBy(Guid guid);
    }
}
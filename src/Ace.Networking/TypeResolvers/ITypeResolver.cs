using System;
using System.IO;
using System.Reflection;

namespace Ace.Networking.TypeResolvers
{
    public interface ITypeResolver
    {
        byte Signature { get; }
        bool TryResolve(Stream stream, out Type type);
        bool TryWrite(Stream stream, Type type);
        void RegisterType(Type type);
        void RegisterAssembly(Assembly assembly);
        void RegisterAssembly(Assembly assembly, params Type[] attributes);
    }
}
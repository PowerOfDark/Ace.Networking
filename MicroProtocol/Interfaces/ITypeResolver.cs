using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface ITypeResolver
    {
        bool TryResolve(Stream stream, out Type type);
        bool TryWrite(Stream stream, Type type);
        void RegisterType(Type type);
        void RegisterAssembly(Assembly assembly);
        void RegisterAssembly(Assembly assembly, params Type[] attributes);
        byte Signature { get; }
    }
}

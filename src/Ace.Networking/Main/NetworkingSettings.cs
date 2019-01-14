using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ace.Networking.Structures;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public static class NetworkingSettings
    {
        public const int BufferSize = 8192;

        public static readonly Type[] Primitives =
        {
            typeof(object), typeof(Stream), typeof(byte), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(bool), typeof(sbyte), typeof(DateTime), typeof(void), typeof(short), typeof(ushort), typeof(double),
            typeof(float), typeof(List<>), typeof(Dictionary<,>)
        };

        private static readonly List<Assembly> _packetAssemblies = new List<Assembly>();

        public static IReadOnlyList<Assembly> PacketAssemblies
        {
            get
            {
                lock (_packetAssemblies)
                {
                    return _packetAssemblies.ToList().AsReadOnly();
                }
            }
        }

        public static ITypeResolver DefaultTypeResolver { get; } = new DeepGuidTypeResolver();

        public static void RegisterPacketAssembly(Assembly assembly)
        {
            lock (_packetAssemblies)
            {
                _packetAssemblies.Add(assembly);
            }
        }
    }
}
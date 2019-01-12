using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.TypeResolvers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking
{
    public static class NetworkingSettings
    {
        public static ITypeResolver DefaultTypeResolver { get; } = new DeepGuidTypeResolver();
    }
}

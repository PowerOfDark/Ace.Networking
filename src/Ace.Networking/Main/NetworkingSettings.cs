using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public static class NetworkingSettings
    {
        public const int BufferSize = 8192;
        public static ITypeResolver DefaultTypeResolver { get; } = new DeepGuidTypeResolver();
    }
}
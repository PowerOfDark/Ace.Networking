using System;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class EntanglementServiceExtension
    {
        public static IServicesBuilder<T> AddEntanglementHost<T>(this IServicesBuilder<T> services,
            Action<IEntanglementHostService> config = null) where T : class, ICommon
        {
            var instance = new EntanglementHostService();
            services.Add<IEntanglementHostService, EntanglementHostService>(instance, config);
            config?.Invoke(instance);
            return services;
        }

        public static IServicesBuilder<T> AddEntanglementClient<T>(this IServicesBuilder<T> services,
            Action<IEntanglementClientService> config = null) where T : class, IConnection
        {
            var instance = new EntanglementClientService();
            services.Add<IEntanglementClientService, EntanglementClientService>(instance);
            config?.Invoke(instance);
            return services;
        }
    }
}
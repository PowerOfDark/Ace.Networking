﻿using System;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Threading;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class EntanglementServiceExtension
    {
        public static IServicesBuilder<T> AddEntanglementHost<T>(this IServicesBuilder<T> services,
            Action<IEntanglementHostService> config = null) where T : class, ICommon
        {
            services.Add<IEntanglementHostService, EntanglementHostService>(config);
            return services;
        }

        public static IServicesBuilder<T> AddEntanglementClient<T>(this IServicesBuilder<T> services,
            Action<IEntanglementClientService> config = null) where T : class, IConnection
        {
            services.Add<IEntanglementClientService, EntanglementClientService>(config);
            return services;
        }
    }
}
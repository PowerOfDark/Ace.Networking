﻿using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class EntanglementServiceExtension
    {
        public static IServicesBuilder AddEntanglement(this IServicesBuilder services, Action<IEntanglementService> config = null)
        {
            var instance = new EntanglementService();
            services.Add<IEntanglementService, EntanglementService>(instance);
            config?.Invoke(instance);
            return services;
        }
    }
}

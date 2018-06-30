using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Services
{
    public class BasicServicesBuilder : IServicesBuilder
    {
        private Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        public IReadOnlyDictionary<Type, IService> Services => _services;
        public IServicesBuilder Add<TBase, T>(T instance, Action<T> config = null) where TBase : IService where T : TBase
        {
            _services.Add(typeof(TBase), instance);
            config?.Invoke(instance);
            return this;
        }

        public IInternalServiceManager Build()
        {
            if ((_services?.Count ?? 0) == 0)
            {
                return BasicServiceManager.Empty;
            }

            return new BasicServiceManager(_services);
        }
    }
}

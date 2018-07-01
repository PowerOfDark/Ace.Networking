using System;
using System.Collections.Generic;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public class ServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, IService<TInterface>>
            _services = new Dictionary<Type, IService<TInterface>>();

        public IReadOnlyDictionary<Type, IService<TInterface>> Services => _services;


        public IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config)
            where T : TBase where TBase : IService<TInterface>
        {
            _services.Add(typeof(TBase), instance);
            config?.Invoke(instance);
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            if ((_services?.Count ?? 0) == 0) return ServicesManager<TInterface>.Empty;

            return new ServicesManager<TInterface>(_services);
        }
    }
}
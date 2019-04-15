using System;
using System.Collections.Generic;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public class ServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, object>
            _services = new Dictionary<Type, object>();

        public IReadOnlyDictionary<Type, object> Services => _services;


        public IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            _services.Add(typeof(TBase), instance);
            config?.Invoke(instance);
            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>()
            where T : class, TBase where TBase : class
        {
            Add<TBase, T>(Activator.CreateInstance<T>());
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            if ((_services?.Count ?? 0) == 0) return ServicesManager<TInterface>.Empty;

            return new ServicesManager<TInterface>(_services);
        }
    }
}
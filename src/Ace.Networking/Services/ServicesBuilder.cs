using System;
using System.Collections.Generic;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public class ServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, object>
            _services = new Dictionary<Type, object>();

        private readonly Dictionary<Type, Delegate> 
            _pendingConfigs = new Dictionary<Type, Delegate>();

        private readonly Dictionary<Type, Func<object>>
            _factories = new Dictionary<Type, Func<object>>();

        public IServicesBuilder<TInterface> AddInstance<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            _services.Add(typeof(TBase), instance);
            _pendingConfigs[typeof(T)] = config;
            return this;
        }

        public IServicesBuilder<TInterface> AddInstance<TBase, T>(Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            AddInstance<TBase, T>(Activator.CreateInstance<T>(), config);
            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>(Func<T> factory, Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            _factories.Add(typeof(TBase), factory);
            _pendingConfigs[typeof(T)] = config;
            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>(Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            _factories.Add(typeof(TBase), Activator.CreateInstance<T>);
            _pendingConfigs[typeof(T)] = config;
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            var services = new Dictionary<Type, object>(_services);
            foreach (var factory in _factories)
            {
                try
                {
                    services.Add(factory.Key, factory.Value.Invoke());
                }
                catch
                {
                    //ignored
                }
            }
            foreach(var res in services)
                if (_pendingConfigs.TryGetValue(res.Value.GetType(), out var d))
                    d?.DynamicInvoke(res.Value);
            return new ServicesManager<TInterface>(services);
        }
    }
}
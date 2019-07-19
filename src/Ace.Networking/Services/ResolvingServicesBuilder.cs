using System;
using System.Collections.Generic;
using Ace.Networking.Helpers;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public class ResolvingServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, Delegate> _pendingConfigs = new Dictionary<Type, Delegate>();

        private readonly Dictionary<Type, DependencyResolver.DependencyEntry<object>>
            _servicesMap = new Dictionary<Type, DependencyResolver.DependencyEntry<object>>();

        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        public IServicesBuilder<TInterface> AddInstance<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class
        {
            _servicesMap.Add(typeof(TBase),
                new DependencyResolver.DependencyEntry<object>(typeof(T), instance));
            _pendingConfigs[typeof(T)] = config;

            return this;
        }

        public IServicesBuilder<TInterface> AddInstance<TBase, T>(Action<T> config)
            where T : class, TBase where TBase : class
        {
            AddInstance<TBase, T>(Activator.CreateInstance<T>(), config);
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            if ((_servicesMap?.Count ?? 0) == 0) return ServicesManager<TInterface>.Empty;

            var res = DependencyResolver.Resolve(_servicesMap, _factories);
            foreach (var r in res)
                if (_pendingConfigs.TryGetValue(r.Value.GetType(), out var d))
                    d?.DynamicInvoke(r.Value);
            return new ServicesManager<TInterface>(res);
        }

        public IServicesBuilder<TInterface> Add<TBase, T>(Func<T> factory, Action<T> config = null) where T : class, TBase where TBase : class
        {
            _factories.Add(typeof(TBase), factory);
            _pendingConfigs[typeof(T)] = config;
            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>(Action<T> config = null) where T : class, TBase where TBase : class
        {
            _servicesMap.Add(typeof(TBase),
                new DependencyResolver.DependencyEntry<object>(typeof(T), null));
            _pendingConfigs[typeof(T)] = config;
            return this;
        }
    }
}
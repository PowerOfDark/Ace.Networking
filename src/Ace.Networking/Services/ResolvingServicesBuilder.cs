﻿using System;
using System.Collections.Generic;
using Ace.Networking.Helpers;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public class ResolvingServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, Delegate> _pendingConfigs = new Dictionary<Type, Delegate>();

        private readonly Dictionary<Type, DependencyResolver.DependencyEntry<IService<TInterface>>>
            _servicesMap = new Dictionary<Type, DependencyResolver.DependencyEntry<IService<TInterface>>>();


        public IReadOnlyDictionary<Type, IService<TInterface>> Services => throw new NotSupportedException();


        public IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class, IService<TInterface>
        {
            //if (config != null && instance == null)
            //    throw new NotSupportedException("Config action is not supported in ResolvingServicesBuilder");
            _servicesMap.Add(typeof(TBase),
                new DependencyResolver.DependencyEntry<IService<TInterface>>(typeof(T), instance));
            if (instance != null)
                config?.Invoke(instance);
            else if (config != null) _pendingConfigs[typeof(T)] = config;

            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>()
            where T : class, TBase where TBase : class, IService<TInterface>
        {
            _servicesMap.Add(typeof(TBase),
                new DependencyResolver.DependencyEntry<IService<TInterface>>(typeof(T), null));
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            if ((_servicesMap?.Count ?? 0) == 0) return ServicesManager<TInterface>.Empty;

            var res = DependencyResolver.Resolve(_servicesMap);
            foreach (var r in res)
                if (_pendingConfigs.TryGetValue(r.Value.GetType(), out var d))
                    d.DynamicInvoke(r.Value);
            return new ServicesManager<TInterface>(res);
        }
    }
}
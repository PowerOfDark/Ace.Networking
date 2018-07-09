using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Helpers;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public class ResolvingServicesBuilder<TInterface> : IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        private readonly Dictionary<Type, (Type type, IService<TInterface> instance)>
            _servicesMap = new Dictionary<Type, (Type type, IService<TInterface> instance)>();



        public IReadOnlyDictionary<Type, IService<TInterface>> Services => throw new NotSupportedException();


        public IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class, IService<TInterface>
        {
            if (config != null && instance == null)
                throw new NotSupportedException("Config action is not supported in ResolvingServicesBuilder");
            _servicesMap.Add(typeof(TBase), (typeof(T), instance));
            if (instance != null)
                config?.Invoke(instance);
            return this;
        }

        public IServicesBuilder<TInterface> Add<TBase, T>()
            where T : class, TBase where TBase : class, IService<TInterface>
        {
            _servicesMap.Add(typeof(TBase), (typeof(T), null));
            return this;
        }

        public IInternalServiceManager<TInterface> Build()
        {
            if ((_servicesMap?.Count ?? 0) == 0) return ServicesManager<TInterface>.Empty;

            return new ServicesManager<TInterface>(DependencyResolver.Resolve(_servicesMap));
        }
    }
}

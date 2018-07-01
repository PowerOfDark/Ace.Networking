using System;
using System.Collections.Generic;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public class ServicesManager<TInterface> : IInternalServiceManager<TInterface> where TInterface : class, ICommon
    {
        public static ServicesManager<TInterface> Empty = new ServicesManager<TInterface>();

        protected IReadOnlyDictionary<Type, IService<TInterface>> Services =
            new Dictionary<Type, IService<TInterface>>();

        protected ServicesManager()
        {
        }

        public ServicesManager(IDictionary<Type, IService<TInterface>> services)
        {
            Services = new Dictionary<Type, IService<TInterface>>(services);
        }

        public void Attach(TInterface server)
        {
            lock (Services)
            {
                foreach (var service in Services) service.Value.Attach(server);
            }
        }

        public void Detach(TInterface server)
        {
            lock (Services)
            {
                foreach (var service in Services) service.Value.Detach(server);
            }
        }

        public T Get<T>() where T : class, IService<TInterface>
        {
            if (!Services.TryGetValue(typeof(T), out var s)) return null;
            return (T) s;
        }
    }
}
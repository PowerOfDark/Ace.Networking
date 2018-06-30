using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public class BasicServiceManager : IInternalServiceManager
    {
        public static BasicServiceManager Empty = new BasicServiceManager();

        protected IReadOnlyDictionary<Type, IService> Services = new Dictionary<Type, IService>();

        protected BasicServiceManager() { }

        public BasicServiceManager(IDictionary<Type, IService> services)
        {
            Services = new Dictionary<Type, IService>(services);
        }

        public void Attach(IConnectionDispatcherInteface server)
        {
            lock (Services)
            {
                foreach (var service in Services)
                {
                    service.Value.Attach(server);
                }
            }
        }

        public void Detach(IConnectionDispatcherInteface server)
        {
            lock (Services)
            {
                foreach (var service in Services)
                {
                    service.Value.Detach(server);
                }
            }
        }

        public T Get<T>() where T : class, IService
        {
            if (!Services.TryGetValue(typeof(T), out var s)) return null;
            return (T)s;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public class BasicServiceManager : IInternalServiceManager
    {
        protected Dictionary<Type, IService> Services = new Dictionary<Type, IService>();

        public void Add<TBase, T>(T instance) where T : TBase where TBase : IService
        {
            lock (Services)
            {
                Services.Add(typeof(TBase), instance);
            }
        }

        public void Attach(IServer server)
        {
            lock (Services)
            {
                foreach (var service in Services)
                {
                    if (!service.Value.IsActive)
                        service.Value.Attach(server);
                }
            }
        }

        public void Detach()
        {
            lock (Services)
            {
                foreach (var service in Services)
                {
                    if (service.Value.IsActive)
                        service.Value.Detach();
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

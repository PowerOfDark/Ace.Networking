using System;
using System.Collections.Generic;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public class ServicesManager<TInterface> : IInternalServiceManager<TInterface> where TInterface : class, ICommon
    {
        public static ServicesManager<TInterface> Empty = new ServicesManager<TInterface>();

        protected IReadOnlyDictionary<Type, object> Services =
            new Dictionary<Type, object>();

        protected ServicesManager()
        {
        }

        public ServicesManager(IDictionary<Type, object> services)
        {
            Services = new Dictionary<Type, object>(services);
        }

        public void Attach(TInterface client)
        {
            lock (Services)
            {
                foreach (var kv in Services)
                {
                    var service = kv.Value;
                    if(service is IService<ICommon> icommon)
                    {
                        icommon.Attach(client);
                    }
                    else if(service is IService<IServer> iserver && client is IServer server)
                    {
                        iserver.Attach(server);
                    }
                    else if(service is IService<IConnection> icon && client is IConnection con)
                    {
                        icon.Attach(con);
                    }
                }
            }
        }

        public void Detach(TInterface client)
        {
            lock (Services)
            {
                foreach (var kv in Services)
                {
                    var service = kv.Value;
                    if (service is IService<ICommon> icommon)
                    {
                        icommon.Detach(client);
                    }
                    else if (service is IService<IServer> iserver && client is IServer server)
                    {
                        iserver.Detach(server);
                    }
                    else if (service is IService<IConnection> icon && client is IConnection con)
                    {
                        icon.Detach(con);
                    }
                }
            }
        }

        public T Get<T>() where T : class
        {
            if (!Services.TryGetValue(typeof(T), out var s)) return null;
            return (T) s;
        }
    }
}
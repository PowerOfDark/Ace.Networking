using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Structures
{
    public class ConnectionGroup : IConnectionGroup
    {
        private readonly HashSet<IConnection> _clients = new HashSet<IConnection>();
        public IEnumerable<IConnection> Clients => _clients;

        protected void OnDisconnected(IConnection connection, Exception ex)
        {
            Disconnected?.Invoke(connection, ex);
        }

        public void AddClient(IConnection client)
        {
            lock (_clients)
            {
                if (!_clients.Contains(client))
                {
                    _clients.Add(client);
                    client.Disconnected += OnDisconnected;
                }
            }
        }

        public void Close()
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                }
            }
        }

        public bool ContainsClient(IConnection client)
        {
            lock (_clients)
            {
                return _clients.Contains(client);
            }
        }

        public bool RemoveClient(IConnection client)
        {
            lock (_clients)
            {
                if (_clients.Remove(client))
                {
                    client.Disconnected -= OnDisconnected;
                    return true;
                }
            }
            return false;
        }

        public Task Send<T>(T data)
        {
            var tasks = new List<Task>();
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    tasks.Add(client.Send<T>(data));
                }
            }

            return Task.WhenAll(tasks);
        }

        public void OnRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.OnRequest<T>(handler);
                }
            }
        }

        public bool OffRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler)
        {
            bool ret = false;
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    ret |= client.OffRequest<T>(handler);
                }
            }
            return ret;
        }

        public void On<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.On<T>(handler);
                }
            }
        }

        public bool Off<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler)
        {
            bool ret = false;
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    ret |= client.Off<T>(handler);
                }
            }
            return ret;
        }

        public event Connection.DisconnectHandler Disconnected;
    }
}

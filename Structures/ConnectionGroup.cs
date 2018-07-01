using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Structures
{
    public class ConnectionGroup : IConnectionGroup
    {
        private readonly HashSet<IConnection> _clients = new HashSet<IConnection>();
        public IReadOnlyCollection<IConnection> Clients => _clients;

        public void AddClient(IConnection client)
        {
            lock (_clients)
            {
                if (!_clients.Contains(client))
                {
                    _clients.Add(client);
                    client.ClientDisconnected += OnDisconnected;
                }
            }
        }

        public void Close()
        {
            lock (_clients)
            {
                foreach (var client in _clients) client.Close();
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
                    client.ClientDisconnected -= OnDisconnected;
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
                    if (client.Connected)
                        tasks.Add(client.Send(data));
            }

            return Task.WhenAll(tasks);
        }

        public void OnRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        client.OnRequest<T>(handler);
            }
        }

        public bool OffRequest<T>(PayloadHandlerDispatcherBase.RequestHandler handler)
        {
            var ret = false;
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        ret |= client.OffRequest<T>(handler);
            }

            return ret;
        }

        public void On<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        client.On(handler);
            }
        }

        public bool Off<T>(PayloadHandlerDispatcherBase.GenericPayloadHandler<T> handler)
        {
            var ret = false;
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        ret |= client.Off(handler);
            }

            return ret;
        }

        public event Connection.DisconnectHandler ClientDisconnected;

        protected void OnDisconnected(IConnection connection, Exception ex)
        {
            RemoveClient(connection);
            ClientDisconnected?.Invoke(connection, ex);
        }
    }
}
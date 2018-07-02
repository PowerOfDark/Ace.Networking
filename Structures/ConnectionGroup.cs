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
                    client.PayloadReceived += OnPayloadReceived;
                    client.ClientDisconnected += OnDisconnected;
                }
            }
        }

        public void Close()
        {
            DoForEach(t => t.Close());
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
                    client.PayloadReceived -= OnPayloadReceived;
                    client.ClientDisconnected -= OnDisconnected;
                    return true;
                }
            }

            return false;
        }

        public Task Send<T>(T data)
        {
            return Task.WhenAll(DoForEach((client, tasks) => tasks.Add(client.Send(data)),
                new List<Task>(Clients.Count)));
        }

        public void OnRequest<T>(RequestHandler handler)
        {
            DoForEach(client => client.OnRequest<T>(handler));
        }

        public bool OffRequest<T>(RequestHandler handler)
        {
            return DoForEach((client, any) => any | client.OffRequest<T>(handler), false);
        }

        public void On<T>(GenericPayloadHandler<T> handler)
        {
            DoForEach(client => client.On(handler));
        }

        public bool Off<T>(GenericPayloadHandler<T> handler)
        {
            return DoForEach((client, any) => any | client.Off(handler), false);
        }

        public void On(Type type, PayloadHandler handler)
        {
            DoForEach(client => client.On(type, handler));
        }

        public void On<T>(PayloadHandler handler)
        {
            DoForEach(client => client.On<T>(handler));
        }

        public bool Off(Type type, PayloadHandler handler)
        {
            return DoForEach((client, any) => any | client.Off(type, handler), false);
        }

        public bool Off<T>(PayloadHandler handler)
        {
            return DoForEach((client, any) => any | client.Off<T>(handler), false);
        }

        public bool Off(Type type)
        {
            return DoForEach((client, any) => any | client.Off(type), false);
        }

        public bool Off<T>()
        {
            return DoForEach((client, any) => any | client.Off<T>(), false);
        }

        public void OnRequest(Type type, RequestHandler handler)
        {
            DoForEach(client => client.OnRequest(type, handler));
        }

        public bool OffRequest(Type type)
        {
            return DoForEach((client, any) => any | client.OffRequest(type), false);
        }

        public bool OffRequest(Type type, RequestHandler handler)
        {
            return DoForEach((client, any) => any | client.OffRequest(type, handler), false);
        }

        public bool OffRequest<T>()
        {
            return DoForEach((client, any) => any | client.OffRequest<T>(), false);
        }

        public event GlobalPayloadHandler PayloadReceived;
        public event Connection.DisconnectHandler ClientDisconnected;

        protected T DoForEach<T>(Action<IConnection, T> action, T variable = default) where T : class
        {
            var res = variable;
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        action.Invoke(client, res);
            }

            return res;
        }

        protected T DoForEach<T>(Func<IConnection, T, T> action, T variable = default)
        {
            var res = variable;
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        res = action.Invoke(client, res);
            }

            return res;
        }

        protected void DoForEach(Action<IConnection> action)
        {
            lock (_clients)
            {
                foreach (var client in _clients)
                    if (client.Connected)
                        action.Invoke(client);
            }
        }

        protected void OnDisconnected(IConnection connection, Exception ex)
        {
            RemoveClient(connection);
            ClientDisconnected?.Invoke(connection, ex);
        }

        protected void OnPayloadReceived(IConnection connection, object payload, Type type)
        {
            PayloadReceived?.Invoke(connection, payload, type);
        }
    }
}
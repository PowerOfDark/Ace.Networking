using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ace.Networking.Handlers;
using Ace.Networking.Serializers;
using Ace.Networking.Services;
using Ace.Networking.Threading;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking.Structures
{
    public class ConnectionGroup : PayloadHandlerDispatcher, IConnectionGroup
    {
        private readonly HashSet<IConnection> _clients = new HashSet<IConnection>();


        public ConnectionGroup(ICommon host)
        {
            Host = host;
        }

        public IReadOnlyCollection<IConnection> Clients => _clients;
        public ICommon Host { get; }
        public IServiceManager Services => Host.Services;
        public ITypeResolver TypeResolver => Host?.TypeResolver;
        public IPayloadSerializer Serializer => Host?.Serializer;
        public event Connection.InternalPayloadDispatchHandler DispatchPayload;

        public void AddClient(IConnection client)
        {
            lock (_clients)
            {
                if (!_clients.Contains(client)) _clients.Add(client);

                if (_clients.Count == 1) Bind();
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
                    if (_clients.Count == 0) Unbind();
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


        public event GlobalPayloadHandler PayloadReceived;
        public event Connection.DisconnectHandler ClientDisconnected;

        internal void Bind()
        {
            Host.DispatchPayload += Host_DispatchPayload;
            Host.ClientDisconnected += Host_ClientDisconnected;
        }

        private void Host_ClientDisconnected(IConnection connection, Exception exception)
        {
            if (!ContainsClient(connection)) return;
            OnDisconnected(connection, exception);
        }

        internal void Unbind()
        {
            Host.DispatchPayload -= Host_DispatchPayload;
            Host.ClientDisconnected -= Host_ClientDisconnected;
        }

        private bool Host_DispatchPayload(IConnection connection, object payload, Type type,
            Action<object> responseSender, int? requestId)
        {
            if (!ContainsClient(connection)) return false;

            var ret = ProcessPayloadHandlers(connection, payload, type, responseSender, requestId);


            try
            {
                PayloadReceived?.Invoke(connection, payload, type);
            }
            catch
            {
            }


            if (DispatchPayload != null)
                foreach (var handler in DispatchPayload.GetInvocationList())
                    try
                    {
                        if (handler is Connection.InternalPayloadDispatchHandler h)
                            ret |= h.Invoke(connection, payload, type, responseSender, requestId);
                    }
                    catch
                    {
                    }

            return ret;
        }

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
    }
}
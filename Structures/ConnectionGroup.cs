using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Structures
{
    public class ConnectionGroup : IConnectionGroup
    {
        private HashSet<IConnection> _clients = new HashSet<IConnection>();
        public IEnumerable<IConnection> Clients => throw new NotImplementedException();

        public void AddClient(IConnection client)
        {
            lock (_clients)
            {
                if (!_clients.Contains(client))
                    _clients.Add(client);
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
                return _clients.Remove(client);
            }
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

    }
}

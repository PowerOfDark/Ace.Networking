using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking
{
    public static class Extensions
    {
        public static bool RemoveFirst<T>(this LinkedList<T> list, Func<T, bool> pred)
        {
            var iterator = list.First;
            while (iterator != null)
            {
                var next = iterator.Next;
                if (pred(iterator.Value))
                {
                    list.Remove(iterator);
                    return true;
                }
                iterator = next;
            }
            return false;
        }

        public static Connection ToConnection(this TcpClient client, ProtocolConfiguration configuration,
            ISslStreamFactory ssl = null)
        {
            return new Connection(client, configuration, ssl);
        }

        public static bool TryAddLast<TKey, TValue>(this ConcurrentDictionary<TKey, LinkedList<TValue>> dict, TKey key,
            TValue val)
        {
            var ret = true;
            LinkedList<TValue> list;
            if (!dict.TryGetValue(key, out list))
            {
                ret = dict.TryAdd(key, list = new LinkedList<TValue>());
                if (!ret)
                {
                    ret = dict.TryGetValue(key, out list);
                }
            }
            if (ret)
            {
                lock (list)
                {
                    list.AddLast(val);
                }
            }
            return ret;
        }

        public static bool TryEnqueue<TKey, TValue>(this ConcurrentDictionary<TKey, Queue<TValue>> dict, TKey key,
            TValue val, int capacity = 2)
        {
            var ret = true;
            Queue<TValue> queue;
            if (!dict.TryGetValue(key, out queue))
            {
                ret = dict.TryAdd(key, queue = new Queue<TValue>(capacity));
                if (!ret)
                {
                    ret = dict.TryGetValue(key, out queue);
                }
            }
            if (ret)
            {
                lock (queue)
                {
                    queue.Enqueue(val);
                }
            }
            return ret;
        }
    }
}
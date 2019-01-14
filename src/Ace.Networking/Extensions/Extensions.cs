using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ace.Networking.Extensions
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

        public static void Append<TKey, TValue>(this IDictionary<TKey, LinkedList<TValue>> d, TKey key, TValue val)
        {
            LinkedList<TValue> list;
            if (!d.TryGetValue(key, out list))
                d[key] = list = new LinkedList<TValue>();
            list.AddLast(val);
        }

        public static void AddLast<TKey, TValue>(this ConcurrentDictionary<TKey, LinkedList<TValue>> dict, TKey key,
            TValue val)
        {
            var list = dict.GetOrAdd(key, k => new LinkedList<TValue>());

            lock (list)
            {
                list.AddLast(val);
            }
        }

        public static void Enqueue<TKey, TValue>(this ConcurrentDictionary<TKey, Queue<TValue>> dict, TKey key,
            TValue val, int capacity = 2)
        {
            var queue = dict.GetOrAdd(key, k => new Queue<TValue>(capacity));

            lock (queue)
            {
                queue.Enqueue(val);
            }
        }

        public static CancellationToken? GetCancellationToken(this TimeSpan? ts)
        {
            if (ts.HasValue)
                return new CancellationTokenSource(ts.Value).Token;
            return null;
        }

        public static CancellationToken GetCancellationToken(this TimeSpan ts)
        {
            return new CancellationTokenSource(ts).Token;
        }
    }
}
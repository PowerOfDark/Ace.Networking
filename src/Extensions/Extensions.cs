using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        public static bool TryAddLast<TKey, TValue>(this ConcurrentDictionary<TKey, LinkedList<TValue>> dict, TKey key,
            TValue val)
        {
            var ret = true;
            LinkedList<TValue> list;
            if (!dict.TryGetValue(key, out list))
            {
                ret = dict.TryAdd(key, list = new LinkedList<TValue>());
                if (!ret) ret = dict.TryGetValue(key, out list);
            }

            if (ret)
                lock (list)
                {
                    list.AddLast(val);
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
                if (!ret) ret = dict.TryGetValue(key, out queue);
            }

            if (ret)
                lock (queue)
                {
                    queue.Enqueue(val);
                }

            return ret;
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
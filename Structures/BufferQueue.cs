using System.Collections.Concurrent;
using System.Threading;

namespace Ace.Networking.Structures
{
    public class BufferQueue<T>
    {
        private readonly ConcurrentQueue<T> _container;
        private readonly AutoResetEvent _lock;
        private object _enqueueSyncRoot;

        private BufferQueue()
        {
            _container = new ConcurrentQueue<T>();
            _lock = new AutoResetEvent(false);
            _enqueueSyncRoot = new object();
        }

        public BufferQueue(int barrier) : this()
        {
            Barrier = barrier;
        }

        public int Barrier { get; set; }
        public int Count => _container.Count;

        /// <summary>
        ///     Tries to enqueue <c>item</c> into the internal Queue container.
        ///     If the Queue already contains <c>Barrier</c> items, blocks
        ///     the calling thread until an item has been dequeued.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Enqueue(T item)
        {
            //lock (_enqueueSyncRoot)
            {
                if (Count >= Barrier)
                {
                    _lock.WaitOne();
                }
                _container.Enqueue(item);
            }
        }

        public bool TryDequeue(out T item)
        {
            var res = _container.TryDequeue(out item);
            if (res && Count == Barrier - 1)
            {
                _lock.Set();
            }
            return res;
        }
    }
}
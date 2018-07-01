using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ace.Networking.Structures
{
    public class StaticThreadDisposableAdapter<T> : IDisposable where T : IDisposable
    {
        private static readonly ConcurrentDictionary<int, T> Map = new ConcurrentDictionary<int, T>();

        public StaticThreadDisposableAdapter(T item)
        {
            Item = item;
            Map.TryAdd(Thread.CurrentThread.ManagedThreadId, item);
        }

        public T Item { get; }

        public static T Get()
        {
            return Map[Thread.CurrentThread.ManagedThreadId];
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Item?.Dispose();
                    Map.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GlobalThreadDisposableAdapter() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
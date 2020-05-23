using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.Threading
{
    public class SemaphoreQueue
    {
        private SemaphoreSlim _semaphore;
        private ConcurrentQueue<TaskCompletionSource<bool>> _queue =
            new ConcurrentQueue<TaskCompletionSource<bool>>();
        public SemaphoreQueue(int initialCount)
        {
            _semaphore = new SemaphoreSlim(initialCount);
        }
        public SemaphoreQueue(int initialCount, int maxCount)
        {
            _semaphore = new SemaphoreSlim(initialCount, maxCount);
        }
        public void Wait()
        {
            WaitAsync().Wait();
        }
        public Task WaitAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(tcs);
            _semaphore.WaitAsync().ContinueWith(t =>
            {
                TaskCompletionSource<bool> popped;
                if (_queue.TryDequeue(out popped))
                    popped.TrySetResult(true);
            });
            return tcs.Task;
        }

        public Task WaitAsync(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(tcs);
            _semaphore.WaitAsync(token).ContinueWith(t =>
            {
                TaskCompletionSource<bool> popped;
                if (_queue.TryDequeue(out popped))
                {
                    if (t.IsFaulted)
                        popped.TrySetException(t.Exception);
                    else if (t.IsCanceled)
                        popped.TrySetCanceled();
                    else
                        popped.TrySetResult(true);
                }
            });
            return tcs.Task;

        }
        public void Release()
        {
            _semaphore.Release();
        }
    }
}

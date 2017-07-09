using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.Helpers
{
    public static class TaskHelper
    {
        public static TaskCompletionSource<T> CreateTaskCompletionSource<T>(object state = null, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<T>(state);
            if (timeout.HasValue)
            {
                var cts = new CancellationTokenSource(timeout.Value);
                cts.Token.Register(t =>
                {
                    var task = (TaskCompletionSource<T>)t;
                    task.TrySetCanceled();
                }, tcs);
            }
            return tcs;
        }
    }
}

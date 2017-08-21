using System;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Threading
{
    public class DisposableThreadedQueueProcessor<TItem, TDisposable> : ThreadedQueueProcessor<TItem> where TDisposable : IDisposable
    {
        protected Func<ThreadData, TDisposable> _createContext;

        public DisposableThreadedQueueProcessor(ThreadedQueueProcessorParameters parameters, IWorker<TItem> worker,
            Func<ThreadData, TDisposable> createContext) : base(parameters, worker)
        {
            _createContext = createContext;
        }

        protected override void WorkWrapper(object state)
        {
            if (_createContext != null)
            {
                var data = (ThreadData) state;
                var ctx = _createContext.Invoke(data);

                using (ctx)
                {
                    base.WorkWrapper(state);
                }
            }
            else
            {
                base.WorkWrapper(state);
            }
        }
    }
}
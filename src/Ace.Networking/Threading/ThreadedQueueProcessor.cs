using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ace.Networking.Threading
{
    public class ThreadedQueueProcessor<TItem>
    {
        private readonly ManualResetEvent _enqueueHandle = new ManualResetEvent(true);
        private readonly ManualResetEvent _modifyHandle = new ManualResetEvent(true);

        private readonly object _threadLock = new object();

        protected readonly ThreadedQueueProcessorParameters Parameters;

        public readonly List<ThreadData> ThreadList;
        private volatile bool _freeze;
        private volatile bool _killing;
        private int _pending;
        private Timer _timer;
        protected int BoostPeak;
        protected volatile int ClientCount;
        protected int LastBoostSize;
        protected long LastBoostTick;
        protected long LastKillTick;
        protected long LastStepdownBarrierTick;
        protected long LastStepdownTick;

        protected int LastTickQueueSize = 0;
        protected long LastWorkTicks;
        protected long MonitorTick;
        protected ConcurrentQueue<ThreadedQueueItem<TItem>>[] SendQueues;

        protected volatile int ThreadCount;

        protected IWorker<TItem> Worker;
        protected AutoResetEvent WorkHandle = new AutoResetEvent(false);

        protected long WorkTicks = 0;

        public ThreadedQueueProcessor(ThreadedQueueProcessorParameters parameters, IWorker<TItem> worker)
        {
            Parameters = parameters;
            ThreadList = new List<ThreadData>(Parameters.MaxThreads);
            Worker = worker;
        }

        public int Pending => _pending;

        public int Threads => ThreadCount;

        protected long WorkTicksDelta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var ret = WorkTicks - LastWorkTicks;
                LastWorkTicks = WorkTicks;
                return ret;
            }
        }

        public void Initialize()
        {
            if (ThreadCount != 0) return;
            SendQueues = new ConcurrentQueue<ThreadedQueueItem<TItem>>[Parameters.MaxThreads];
            for (var i = 0; i < Parameters.MaxThreads; i++)
            {
                SendQueues[i] = new ConcurrentQueue<ThreadedQueueItem<TItem>>();
                ThreadList.Add(null);
            }

            TrySpawnNewThreads(Parameters.MinThreads);

            if (Parameters.MaxThreads == Parameters.MinThreads)
            {
                // light
            }
            else
            {
                _timer = new Timer(Monitor, null, Timeout.Infinite, Timeout.Infinite);
                _timer.Change(0, Timeout.Infinite);
            }
        }

        public void NewClient()
        {
            if (_timer == null) Initialize();
            Interlocked.Increment(ref ClientCount);
        }

        [Obsolete]
        public void Stop()
        {
            _timer?.Dispose();
            var tmp = ThreadList.ToList();
            ThreadList.Clear(); // crash every enqueue attempt
            while (_pending != 0) Thread.Sleep(1);
            for (var i = 0; i < ThreadCount; i++)
            {
                tmp[i].Run = false;
                tmp[i].WaitHandle.Set();
            }

            tmp.Clear();
            SendQueues = null;
            ThreadCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMinClientThreadCount()
        {
            return ClientCount / Parameters.ClientsPerThread + 1;
        }

        private void Monitor(object state)
        {
            //Console.Title = $"{Pending}";
            var currentThreadCount = ThreadCount;
            var threadsRequired = Math.Max(GetMinClientThreadCount(), Parameters.MinThreads);
            var clients = ClientCount;
            var target = currentThreadCount;

            var cc = 0; //_pending;
            for (var i = 0; i < currentThreadCount; i++) cc = Math.Max(cc, SendQueues[i].Count);

            if (cc >= BoostPeak + Parameters.BoostBarrier &&
                MonitorTick - LastBoostTick >= Parameters.BoostCooldownTicks)
            {
                target += LastBoostSize += 1;
                BoostPeak = cc - Parameters.BoostBarrier;
            }

            if (target < threadsRequired) target = threadsRequired;
            if (target > Parameters.MaxThreads) target = Parameters.MaxThreads;
            if (Parameters.MaxThreadsPerClient.HasValue && target > clients * Parameters.MaxThreadsPerClient.Value)
                target = clients;

            var toSpawn = Math.Max(0, target - currentThreadCount);
            if (toSpawn > 0)
            {
                LastBoostTick = MonitorTick;
                lock (_threadLock)
                {
                    SpawnNewThreads(toSpawn);
                    var tc = ThreadCount;
                    if (Parameters.PreservePartitioning)
                    {
                        _freeze = true;
                        _modifyHandle.Reset();
                        var count = SendQueues.Take(currentThreadCount).Select(t => t.Count).ToList();
                        for (var i = 0; i < currentThreadCount; i++)
                        {
                            var items = count[i];
                            var q = SendQueues[i];
                            for (var j = 0; j < items; j++)
                            {
                                if (!q.TryDequeue(out var item)) continue;
                                SendQueues[Math.Abs(item.Discriminator % tc)].Enqueue(item);
                            }
                        }

                        _freeze = false;
                        _modifyHandle.Set();
                        for (var i = 0; i < tc; i++) ThreadList[i].WaitHandle.Set();
                    }
                }
            }
            else
            {
                if (currentThreadCount > threadsRequired)
                {
                    LastBoostSize = 0;
                    if (MonitorTick - LastKillTick >= Parameters.ThreadKillCooldownTicks)
                    {
                        var leastWork = long.MaxValue;
                        var leastWorkId = -1;
                        var killed = false;

                        lock (_threadLock)
                        {
                            for (var i = threadsRequired; i < currentThreadCount; i++)
                            {
                                var delta = ThreadList[i].WorkTicksDelta;
                                // if the thread has enqueued jobs or is under start-protection
                                if (SendQueues[i].Count != 0 || MonitorTick - ThreadList[i].StartTick <=
                                    Parameters.ThreadStartProtectionTicks)
                                    continue;

                                if (delta < leastWork)
                                {
                                    leastWork = delta;
                                    leastWorkId = i;
                                }


                                if (MonitorTick - ThreadList[i].LastWorkTick >= Parameters.ThreadStopIdleTicks)
                                {
                                    LastKillTick = MonitorTick;
                                    KillThread(i, currentThreadCount);
                                    currentThreadCount--;
                                    killed = true;
                                    break;
                                }
                            }

                            if (!killed && leastWorkId != -1 && MonitorTick - LastBoostTick >= Parameters.StepdownDelay
                                && MonitorTick - LastStepdownTick >= Parameters.StepdownCooldownTicks)
                            {
                                LastKillTick = MonitorTick;
                                LastStepdownTick = MonitorTick;
                                KillThread(leastWorkId, currentThreadCount);
                                currentThreadCount--;
                                killed = true;
                            }
                        }
                    }
                    else
                    {
                        //can't kill...
                        if (MonitorTick - LastBoostTick >= Parameters.StepdownDelay / 2
                            && MonitorTick - LastStepdownBarrierTick >= Parameters.StepdownBarrierTicks)
                        {
                            LastStepdownBarrierTick = MonitorTick;
                            BoostPeak = Math.Max(0, BoostPeak - Parameters.BoostBarrier);
                        }
                    }
                }
                else
                {
                    BoostPeak = 0;
                }
            }

            Interlocked.Increment(ref MonitorTick);
            _timer.Change(1000 / ThreadedQueueProcessorParameters.MonitorTickrate, Timeout.Infinite);
        }

        private void KillThread(int id, int count)
        {
            _modifyHandle.Reset();
            _killing = true;
            ThreadList[id].Run = false;
            ThreadList[id].WaitHandle.Set();
            ThreadList[id] = ThreadList[count - 1];
            SendQueues[id] = SendQueues[count - 1];

            Interlocked.Decrement(ref ThreadCount);
            ThreadList[count - 1] = null;
            _killing = false;
            _modifyHandle.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveClient()
        {
            Interlocked.Decrement(ref ClientCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrySpawnNewThreads(int count = 1)
        {
            lock (_threadLock)
            {
                SpawnNewThreads(count);
            }
        }

        protected void SpawnNewThreads(int count = 1)
        {
            var tick = MonitorTick;
            for (var i = 0; i < count; i++)
            {
                var t = new Thread(WorkWrapper)
                {
                    IsBackground = true,
                };
                var data = new ThreadData
                {
                    Thread = t,
                    WaitHandle = new AutoResetEvent(false),
                    Id = ThreadCount,
                    StartTick = tick
                };
                ThreadList[data.Id] = data;
                t.Start(data);
                Interlocked.Increment(ref ThreadCount);
                //Threads.TryAdd(t.ManagedThreadId, data);
            }
        }

        public void Enqueue(TItem item, int discriminator)
        {
            if (_killing || _freeze) _modifyHandle.WaitOne();
            if (_pending > Parameters.QueueCapacity) _enqueueHandle.WaitOne();

            var i = Math.Abs(discriminator % ThreadCount);
            SendQueues[i].Enqueue(new ThreadedQueueItem<TItem> {Item = item, Discriminator = (ushort) discriminator});
            Interlocked.Increment(ref _pending);
            ThreadList[i].WaitHandle.Set();
        }

        private void WorkMain(ThreadData state)
        {
            //Console.WriteLine("Main thread started");
            Thread.CurrentThread.Name = "WorkMain";
            var data = state;
            var q = SendQueues[data.Id];
            var handle = data.WaitHandle;
            var barrier = Parameters.QueueCapacity;
            var cState = true;
            while (data.Run)
                if (!_freeze && q.TryDequeue(out var item))
                {
                    try
                    {
                        Worker.DoWork(item.Item);
                    }
                    catch
                    {
                        // ignored
                    }

                    if (Interlocked.Decrement(ref _pending) <= barrier)
                    {
                        if (!cState)
                        {
                            _enqueueHandle.Set();
                            cState = true;
                        }
                    }
                    else
                    {
                        if (cState)
                        {
                            _enqueueHandle.Reset();
                            cState = false;
                        }
                    }

                    Interlocked.Increment(ref data.WorkTicks);
                    data.LastWorkTick = MonitorTick;
                }
                else
                {
                    while (!handle.WaitOne(1))
                        if (_pending <= barrier)
                        {
                            if (!cState)
                            {
                                _enqueueHandle.Set();
                                cState = true;
                            }
                        }
                        else
                        {
                            if (cState)
                            {
                                _enqueueHandle.Reset();
                                cState = false;
                            }
                        }
                }
        }

        protected virtual void WorkWrapper(object state)
        {
            var st = (ThreadData) state;
            if (st.Id == 0)
                WorkMain(st);
            else
                Work(st);
        }

        private void Work(ThreadData state)
        {
            //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started");
            var data = state;
            var q = SendQueues[data.Id];
            var handle = data.WaitHandle;
            while (data.Run)
                if (!_freeze && q.TryDequeue(out var item))
                {
                    try
                    {
                        Worker.DoWork(item.Item);
                    }
                    catch
                    {
                        // ignored
                    }

                    Interlocked.Decrement(ref _pending);
                    Interlocked.Increment(ref data.WorkTicks);
                    data.LastWorkTick = MonitorTick;
                }
                else
                {
                    handle.WaitOne();
                }

            //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} killed");
        }
    }
}
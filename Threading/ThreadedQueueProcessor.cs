using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Threading
{
    public class ThreadedQueueProcessor<TItem>
    {
        private readonly ManualResetEvent _enqueueHandle = new ManualResetEvent(true);
        private readonly ManualResetEvent _killingHandle = new ManualResetEvent(true);

        private readonly object _threadLock = new object();

        protected readonly ThreadedQueueProcessorParameters Parameters;

        public readonly List<ThreadData> ThreadList;
        private volatile bool _killing;
        private int _pending;
        private Timer _timer;
        protected int BoostPeak;
        protected volatile int ClientCount;
        protected long LastBoostTick;
        protected long LastKillTick;
        protected long LastStepdownBarrierTick;
        protected long LastStepdownTick;

        protected int LastTickQueueSize = 0;
        protected long LastWorkTicks;
        protected long MonitorTick;
        protected ConcurrentQueue<TItem>[] SendQueues;

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
            if (ThreadCount != 0)
            {
                return;
            }
            SendQueues = new ConcurrentQueue<TItem>[Parameters.MaxThreads];
            for (var i = 0; i < Parameters.MaxThreads; i++)
            {
                SendQueues[i] = new ConcurrentQueue<TItem>();
                ThreadList.Add(null);
            }

            _timer = new Timer(Monitor, null, 1000, 1000 / Parameters.MonitorTickrate);


            SpawnNewThreads(Parameters.MinThreads);
        }

        public void NewClient()
        {
            if (_timer == null)
            {
                Initialize();
            }
            Interlocked.Increment(ref ClientCount);
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

            var cc = _pending;

            if (cc >= BoostPeak + Parameters.BoostBarrier &&
                MonitorTick - LastBoostTick >= Parameters.BoostCooldownTicks)
            {
                target += 1;
                BoostPeak = cc - Parameters.BoostBarrier;
            }
            if (target < threadsRequired)
            {
                target = threadsRequired;
            }
            if (target > Parameters.MaxThreads)
            {
                target = Parameters.MaxThreads;
            }
            if (target > clients)
            {
                target = clients;
            }

            var toSpawn = Math.Max(0, target - currentThreadCount);
            if (toSpawn > 0)
            {
                LastBoostTick = MonitorTick;
                lock (_threadLock)
                {
                    SpawnNewThreads(toSpawn);
                }
            }
            else
            {
                if (currentThreadCount > threadsRequired)
                {
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
                                if (MonitorTick - ThreadList[i].StartTick <= Parameters.ThreadStartProtectionTicks)
                                {
                                    continue;
                                }

                                if (delta < leastWork)
                                {
                                    leastWork = delta;
                                    leastWorkId = i;
                                }


                                if (MonitorTick - ThreadList[i].LastWorkTick >= Parameters.ThreadStopIdleTicks &&
                                    SendQueues[i].Count == 0)
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
        }

        private void KillThread(int id, int count)
        {
            _killingHandle.Reset();
            _killing = true;
            ThreadList[id].Run = false;
            ThreadList[id].WaitHandle.Set();
            ThreadList[id] = ThreadList[count - 1];
            SendQueues[id] = SendQueues[count - 1];

            Interlocked.Decrement(ref ThreadCount);
            ThreadList[count - 1] = null;
            _killing = false;
            _killingHandle.Set();
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
                var t = ThreadCount == 0 ? new Thread(WorkMain) : new Thread(Work);
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
            if (_killing)
            {
                _killingHandle.WaitOne();
            }
            if (_pending > Parameters.QueueCapacity)
            {
                _enqueueHandle.WaitOne();
            }

            var i = discriminator % ThreadCount;
            SendQueues[i].Enqueue(item);
            Interlocked.Increment(ref _pending);
            ThreadList[i].WaitHandle.Set();
        }

        private void WorkMain(object state)
        {
            //Console.WriteLine("Main thread started");
            var data = (ThreadData) state;
            var q = SendQueues[data.Id];
            var handle = data.WaitHandle;
            var barrier = Parameters.QueueCapacity;
            var cState = true;
            while (data.Run)
            {
                if (q.TryDequeue(out var item))
                {
                    try
                    {
                        Worker.DoWork(item);
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
                    while (!handle.WaitOne(5))
                    {
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
            }
        }


        private void Work(object state)
        {
            //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started");
            var data = (ThreadData) state;
            var q = SendQueues[data.Id];
            var handle = data.WaitHandle;
            while (data.Run)
            {
                if (q.TryDequeue(out var item))
                {
                    try
                    {
                        Worker.DoWork(item);
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
                    handle.WaitOne(5);
                }
            }
            //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} killed");
        }
    }
}
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
        private static Timer _timer;
        private static volatile bool _killing;
        private static readonly ManualResetEvent KillingHandle = new ManualResetEvent(true);

        private readonly object _threadLock = new object();

        protected readonly ThreadedQueueProcessorParameters Parameters;

        public readonly List<ThreadData> ThreadList;
        private int _pending;
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

        public void Assign(Connection connection)
        {
            if (_timer == null)
            {
                Initialize();
            }
            Interlocked.Increment(ref ClientCount);
            if (!connection.Connected)
            {
                Connection_Disconnected(connection, new Exception());
            }
            else
            {
                connection.Disconnected += Connection_Disconnected;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMinClientThreadCount()
        {
            return ClientCount / Parameters.ClientsPerThread + 1;
        }

        private void Monitor(object state)
        {
            //Console.Title = $"{ThreadCount}";
            var currentThreadCount = ThreadCount;
            var threadsRequired = Math.Max(GetMinClientThreadCount(), Parameters.MinThreads);
            var clients = ClientCount;
            var target = currentThreadCount;

            var cc = Pending;

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
                            for (var i = 0; i < currentThreadCount; i++)
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
            KillingHandle.Reset();
            _killing = true;
            ThreadList[id].Run = false;
            ThreadList[id].WaitHandle.Set();
            ThreadList[id] = ThreadList[count - 1];
            SendQueues[id] = SendQueues[count - 1];

            Interlocked.Decrement(ref ThreadCount);
            ThreadList[count - 1] = null;
            _killing = false;
            KillingHandle.Set();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Connection_Disconnected(Connection connection, Exception exception)
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
                var t = new Thread(Work);
                var data = new ThreadData
                {
                    Thread = t,
                    WaitHandle = new AutoResetEvent(false),
                    Id = ThreadCount,
                    StartTick = MonitorTick
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
                KillingHandle.WaitOne();
            }

            var i = discriminator % ThreadCount;
            SendQueues[i].Enqueue(item);
            Interlocked.Increment(ref _pending);
            ThreadList[i].WaitHandle.Set();
        }

        private void Work(object state)
        {
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
                    }
                    Interlocked.Decrement(ref _pending);
                    Interlocked.Increment(ref data.WorkTicks);
                    data.LastWorkTick = Interlocked.Increment(ref MonitorTick);
                }
                else
                {
                    handle.WaitOne();
                }
            }
        }
    }
}
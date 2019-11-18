using Ace.Networking.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ace.Networking.ThreadBenchmark
{
    public class Program
    {
        public class ThreadedQueueProcessorTests
        {
            public struct DummyWorkItem
            {
                public int Delay;
                public int TargetValue;
                public int Target;
            }

            public class DummyWorker : IWorker<DummyWorkItem>
            {
                private int[] _counters;

                public DummyWorker(int[] counters)
                {
                    _counters = counters;
                }

                public void DoWork(DummyWorkItem item)
                {
                    Thread.Sleep(item.Delay);
                    _counters[item.Target]++;
                    bool isOk = _counters[item.Target] == item.TargetValue;
                    if (!isOk)
                        Console.WriteLine($"Error @{item.Target}: {_counters[item.Target]}, expected {item.TargetValue}");
                    Debug.Assert(isOk);
                }
            }

            internal static ThreadedQueueProcessor<DummyWorkItem> Get(ThreadedQueueProcessorParameters args, int[] counters)
            {
                return new ThreadedQueueProcessor<DummyWorkItem>(args, new DummyWorker(counters));
            }

            internal static void Pipe(ThreadedQueueProcessor<DummyWorkItem> p, int index, int count, int delayMin = 50, int delayMax = 120)
            {
                Console.WriteLine($"Pipe #{index}, {count}");
                p.NewClient();
                var rnd = new Random();
                for (int i = 0; i < count; i++)
                {
                    p.Enqueue(new DummyWorkItem() { Delay = rnd.Next(delayMin, delayMax), Target = index, TargetValue = i + 1 }, index);
                }
                //p.RemoveClient();
            }


            internal static void Test(ThreadedQueueProcessorParameters args, int count, int size, int delayMin = 50, int delayMax = 120)
            {
                var counters = new int[count];
                var p = Get(args, counters);
                p.Initialize();
                var tasks = new List<Task>();
                for (int i = 0; i < count; i++)
                    tasks.Add(Task.Factory.StartNew((a) => Pipe(p, (int)a, size, delayMin, delayMax), i));
                Task.WaitAll(tasks.ToArray());
                while (p.Pending != 0)
                {
                    Console.WriteLine($"Pending {p.Pending}, {p.Threads}T");
                    Thread.Sleep(50);
                }
                for (int i = 0; i < count; i++)
                    Debug.Assert(counters[i] == size);
                Console.WriteLine("OK");
            }
        }
        public static void Main(string[] args)
        {
            ThreadedQueueProcessorTests.Test(new ThreadedQueueProcessorParameters() { ClientsPerThread = 100, PreservePartitioning = true, BoostCooldownTicks = 50, MinThreads = 16, MaxThreads = 16,}, 100, 200, 5, 20);
            Console.ReadLine();
        }
    }
}

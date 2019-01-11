using System.Runtime.Serialization;

namespace Ace.Networking.Threading
{
    [DataContract]
    public class ThreadedQueueProcessorParameters
    {
        public const int MonitorTickrate = 50;


        [DataMember] public int BoostBarrier = 100;

        [DataMember] public int BoostCooldownTicks = 1*MonitorTickrate;

        [DataMember] public int ClientsPerThread = 100;

        [DataMember] public ushort MaxThreads = 50;

        [DataMember] public int? MaxThreadsPerClient = 1;

        [DataMember] public ushort MinThreads = 1;

        //[DataMember] public int MonitorTickrate = 10;

        [DataMember] public bool PreservePartitioning = true;

        [DataMember] public int QueueCapacity = 1000;

        [DataMember] public int StepdownBarrierTicks = 6*MonitorTickrate;

        [DataMember] public int StepdownCooldownTicks = 15*MonitorTickrate;

        [DataMember] public int StepdownDelay = 600;

        //public const double THREAD_STOP_RATIO = 0.01;
        //public const int THREAD_STOP_MIN_SAMPLE = 5;
        [DataMember] public int ThreadKillCooldownTicks = 2*MonitorTickrate;

        [DataMember] public int ThreadStartProtectionTicks = 10*MonitorTickrate;

        [DataMember] public int ThreadStopIdleTicks = 10*MonitorTickrate;
    }
}
namespace Ace.Networking.Threading
{
    public class ThreadedQueueProcessorParameters
    {
        public const int MonitorTickrate = 50;

        public int BoostBarrier = 100;

        public int BoostCooldownTicks = 1*MonitorTickrate;

        public int ClientsPerThread = 100;

        public ushort MaxThreads = 50;

        public int? MaxThreadsPerClient = 1;

        public ushort MinThreads = 1;

        //[DataMember] public int MonitorTickrate = 10;

        public bool PreservePartitioning = true;

        public int QueueCapacity = 1000;

        public int StepdownBarrierTicks = 6*MonitorTickrate;

        public int StepdownCooldownTicks = 15*MonitorTickrate;

        public int StepdownDelay = 600;

        //public const double THREAD_STOP_RATIO = 0.01;
        //public const int THREAD_STOP_MIN_SAMPLE = 5;
        public int ThreadKillCooldownTicks = 2*MonitorTickrate;

        public int ThreadStartProtectionTicks = 10*MonitorTickrate;

        public int ThreadStopIdleTicks = 10*MonitorTickrate;
    }
}
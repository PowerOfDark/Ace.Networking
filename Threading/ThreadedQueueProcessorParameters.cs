namespace Ace.Networking.Threading
{
    public class ThreadedQueueProcessorParameters
    {
        public int QueueCapacity = 30_000;

        public int BoostBarrier = 100;
        public int BoostCooldownTicks = 10;
        public int ClientsPerThread = 100;
        public int MaxThreads = 50;
        public int MinThreads = 1;
        public int MonitorTickrate = 10;
        public int StepdownBarrierTicks = 200;
        public int StepdownCooldownTicks = 450;

        public int StepdownDelay = 600;

        //public const double THREAD_STOP_RATIO = 0.01;
        //public const int THREAD_STOP_MIN_SAMPLE = 5;
        public int ThreadKillCooldownTicks = 20;

        public int ThreadStartProtectionTicks = 300;
        public int ThreadStopIdleTicks = 300;
    }
}
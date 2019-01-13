using Ace.Networking.Threading;

namespace Ace.Networking
{
    /// <summary>
    ///     Provides a global send-queue that automatically manages multiple threads.
    ///     Can be enabled per-connection by setting <see cref="ProtocolConfiguration.CustomOutcomingMessageQueue" />
    /// </summary>
    /// <remarks>
    ///     <para>This class is beyond the ken of mortals. Don't look down. </para>
    /// </remarks>
    public class GlobalOutcomingMessageQueue
    {
        private static readonly object SingletonLock = new object();
        private static ThreadedQueueProcessor<SendMessageQueueItem> _instance;

        public static ThreadedQueueProcessor<SendMessageQueueItem> Instance
        {
            get
            {
                lock (SingletonLock)
                {
                    return _instance ?? (_instance =
                               new ThreadedQueueProcessor<SendMessageQueueItem>(new ThreadedQueueProcessorParameters(),
                                   new PushSendWorker()));
                }
            }
        }
    }
}
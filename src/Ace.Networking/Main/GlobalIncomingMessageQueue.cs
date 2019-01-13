using Ace.Networking.Threading;

namespace Ace.Networking
{
    /// <summary>
    ///     Provides a global send-queue that automatically manages multiple threads.
    ///     Can be enabled per-connection by setting <see cref="ProtocolConfiguration.CustomIncomingMessageQueue" />
    /// </summary>
    /// <remarks>
    ///     <para>This class is beyond the ken of mortals. Don't look down. </para>
    /// </remarks>
    public class GlobalIncomingMessageQueue
    {
        private static readonly object SingletonLock = new object();
        private static ThreadedQueueProcessor<ReceiveMessageQueueItem> _instance;

        public static ThreadedQueueProcessor<ReceiveMessageQueueItem> Instance
        {
            get
            {
                lock (SingletonLock)
                {
                    return _instance ?? (_instance =
                               new ThreadedQueueProcessor<ReceiveMessageQueueItem>(
                                   new ThreadedQueueProcessorParameters(),
                                   new HandleReceiveWorker()));
                }
            }
        }
    }
}
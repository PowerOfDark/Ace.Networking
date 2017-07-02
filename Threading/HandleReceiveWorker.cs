using Ace.Networking.Interfaces;

namespace Ace.Networking.Threading
{
    public class HandleReceiveWorker : IWorker<ReceiveMessageQueueItem>
    {
        public void DoWork(ReceiveMessageQueueItem item)
        {
            try
            {
                item.PayloadReceived(item.Header, item.Payload, item.Type);
            }
            catch
            {
                // ignored
            }
        }
    }
}
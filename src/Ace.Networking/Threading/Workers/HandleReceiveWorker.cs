namespace Ace.Networking.Threading.Workers
{
    public class HandleReceiveWorker : IWorker<ReceiveMessageQueueItem>
    {
        public void DoWork(ReceiveMessageQueueItem item)
        {
            item.PayloadReceived(item.Header, item.Payload, item.Type);
        }
    }
}
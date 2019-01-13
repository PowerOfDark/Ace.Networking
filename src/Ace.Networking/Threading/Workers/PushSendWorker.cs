namespace Ace.Networking.Threading.Workers
{
    public class PushSendWorker : IWorker<SendMessageQueueItem>
    {
        public void DoWork(SendMessageQueueItem item)
        {
            if (item.Target.Connected)
                item.Target.PushSendSync(item.Task);
            /* Any exception should be handled by the connection itself,
             * sending to a shut-down connection is the only exception */
        }
    }
}
using System.Threading.Tasks;
namespace Ace.Networking.Threading.Workers
{
    public class SendMessageQueueItem
    {
        public SendMessageQueueItem(Connection target, TaskCompletionSource<object> task)
        {
            Target = target;
            Task = task;
        }

        public Connection Target { get; set; }
        public TaskCompletionSource<object> Task { get; set; }
    }
}
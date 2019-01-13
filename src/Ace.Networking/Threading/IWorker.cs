namespace Ace.Networking.Threading
{
    public interface IWorker<in TItem>
    {
        void DoWork(TItem item);
    }
}
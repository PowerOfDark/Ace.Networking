namespace Ace.Networking.Interfaces
{
    public interface IWorker<in TItem>
    {
        void DoWork(TItem item);
    }
}
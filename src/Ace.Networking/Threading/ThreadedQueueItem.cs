namespace Ace.Networking.Threading
{
    public struct ThreadedQueueItem<TItem>
    {
        public ushort Discriminator;
        public TItem Item;
    }
}
using System.Collections;
using System.Collections.Generic;

namespace Ace.Networking.Structures
{
    public class HistoryList<TItem> : IEnumerable<TItem>
    {
        public readonly LinkedList<TItem> Container;

        public HistoryList(int barrier)
        {
            Container = new LinkedList<TItem>();
            Barrier = barrier;
        }

        public int Barrier { get; set; }

        public IEnumerator<TItem> GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Container.GetEnumerator();
        }

        public void Add(TItem item)
        {
            Container.AddLast(item);
            if (Container.Count > Barrier) Container.RemoveFirst();
        }
    }
}
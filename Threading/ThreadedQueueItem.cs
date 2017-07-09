using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Threading
{
    public struct ThreadedQueueItem<TItem>
    {
        public ushort Discriminator;
        public TItem Item;
    }
}

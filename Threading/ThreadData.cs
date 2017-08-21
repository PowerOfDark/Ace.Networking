using System.Runtime.CompilerServices;
using System.Threading;

namespace Ace.Networking.Threading
{
    public class ThreadData
    {
        public int Id = 0;

        public long LastWorkTick = 0;
        public long LastWorkTicks;
        public bool Run = true;

        public long WorkTicks = 0;

        public AutoResetEvent WaitHandle { get; set; }
        public Thread Thread { get; set; }
        public long StartTick { get; set; }

        public long WorkTicksDelta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var ret = WorkTicks - LastWorkTicks;
                LastWorkTicks = WorkTicks;
                return ret;
            }
        }
    }
}
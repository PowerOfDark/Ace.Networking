using System;
using System.Collections.Generic;
using System.Text;
using Spreads.Utils;

namespace Ace.Networking.Memory
{
    public static class Settings
    {
        public const int SlabLength = 1024 * 128;
        public const int AtomicCounterPoolBucketSize = 1024;
        public const int LARGE_BUFFER_LIMIT = 64 * 1024;
        internal static readonly int ThreadStaticPinnedBufferSize = BitUtil.FindNextPositivePowerOfTwo(LARGE_BUFFER_LIMIT);
    }
}

using System.Buffers;

namespace Ace.Networking.Memory
{
    public class RecyclableMemoryStreamManager
    {
        public double StepCoefficient { get; }
        public ArrayPool<byte> Pool { get; }
        public long BaseSize { get; internal set; }

        public RecyclableMemoryStreamManager(double stepCoefficient, ArrayPool<byte> pool = null, long baseSize = 1024)
        {
            StepCoefficient = stepCoefficient;
            Pool = pool ?? ArrayPool<byte>.Shared;
            BaseSize = baseSize;
        }

        public RecyclableMemoryStream GetStream()
        {
            return new RecyclableMemoryStream(this);
        }

    }
}
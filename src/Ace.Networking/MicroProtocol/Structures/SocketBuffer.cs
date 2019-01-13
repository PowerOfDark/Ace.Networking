using Ace.Networking.Structures;

namespace Ace.Networking.MicroProtocol.Structures
{
    public class SocketBuffer
    {
        public SocketBuffer(int capacity = 65535)
        {
            Buffer = new byte[capacity];
            Capacity = capacity;
            Offset = 0;
        }

        public SocketBuffer(IBufferSlice readBuffer)
        {
            Buffer = readBuffer.Buffer;
            Capacity = readBuffer.Capacity;
            BaseOffset = readBuffer.Offset;
            Offset = readBuffer.Offset;
        }

        public object UserToken { get; set; }


        public int BytesTransferred { get; set; }
        public int Count { get; set; }
        public int Capacity { get; set; }
        public byte[] Buffer { get; set; }
        public int BaseOffset { get; set; }
        public int Offset { get; set; }

        public void SetBuffer(int offset, int count)
        {
            Offset = offset;
            Count = count;
        }

        public void SetBuffer(byte[] buffer, int offset, int count, int capacity)
        {
            Buffer = buffer;
            Count = count;
            Offset = offset;
            Capacity = capacity;
        }

        public void SetBuffer(byte[] buffer, int offset, int count)
        {
            Buffer = buffer;
            Count = count;
            Offset = offset;
            Capacity = count;
        }
    }
}
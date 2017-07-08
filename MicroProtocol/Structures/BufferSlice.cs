using System;
using Ace.Networking.Interfaces;

namespace Ace.Networking.MicroProtocol.Structures
{
    public class BufferSlice : IBufferSlice
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BufferSlice" /> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">Start offset in buffer.</param>
        /// <param name="count">Number of bytes allocated for this slice..</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">offset;Offset+Count must be less than the buffer length.</exception>
        public BufferSlice(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset,
                    "Offset+Count must be less than the buffer length.");
            }

            Capacity = count;
            Offset = offset;
            Buffer = buffer;
        }

        protected BufferSlice()
        {
        }

        /// <summary>
        ///     Where this slice starts
        /// </summary>
        public int Offset { get; }

        /// <summary>
        ///     AMount of bytes allocated for this slice.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        ///     Buffer that this slice is in.
        /// </summary>
        public byte[] Buffer { get; }
    }
}
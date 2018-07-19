// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

// ---------------------------------------------------------------------
// Copyright (c) 2015 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Spreads.Buffers
{
    public static class IntUtil
    {
        /// <summary>
        /// Returns the number of zero bits following the lowest-order ("rightmost")
        /// one-bit in the two's complement binary representation of the specified
        /// {@code int} value.  Returns 32 if the specified value has no
        /// one-bits in its two's complement representation, in other words if it is
        /// equal to zero.
        /// </summary>
        /// <param name="i"> the value whose number of trailing zeros is to be computed </param>
        /// <returns> the number of zero bits following the lowest-order ("rightmost")
        ///     one-bit in the two's complement binary representation of the
        ///     specified {@code int} value, or 32 if the value is equal
        ///     to zero.
        /// @since 1.5 </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NumberOfTrailingZeros(int i)
        {
            // HD, Figure 5-14
            int y;
            if (i == 0)
            {
                return 32;
            }
            int n = 31;
            y = i << 16;
            if (y != 0)
            {
                n = n - 16;
                i = y;
            }
            y = i << 8;
            if (y != 0)
            {
                n = n - 8;
                i = y;
            }
            y = i << 4;
            if (y != 0)
            {
                n = n - 4;
                i = y;
            }
            y = i << 2;
            if (y != 0)
            {
                n = n - 2;
                i = y;
            }
            return n - ((int)((uint)(i << 1) >> 31));
        }

        /// <summary>
        /// Note Olivier: Direct port of the Java method Integer.NumberOfLeadingZeros
        ///
        /// Returns the number of zero bits preceding the highest-order
        /// ("leftmost") one-bit in the two's complement binary representation
        /// of the specified {@code int} value.  Returns 32 if the
        /// specified value has no one-bits in its two's complement representation,
        /// in other words if it is equal to zero.
        ///
        /// <para>Note that this method is closely related to the logarithm base 2.
        /// For all positive {@code int} values x:
        /// &lt;ul&gt;
        /// &lt;li&gt;floor(log&lt;sub&gt;2&lt;/sub&gt;(x)) = {@code 31 - numberOfLeadingZeros(x)}
        /// &lt;li&gt;ceil(log&lt;sub&gt;2&lt;/sub&gt;(x)) = {@code 32 - numberOfLeadingZeros(x - 1)}
        /// &lt;/ul&gt;
        ///
        /// </para>
        /// </summary>
        /// <param name="i"> the value whose number of leading zeros is to be computed </param>
        /// <returns> the number of zero bits preceding the highest-order
        ///     ("leftmost") one-bit in the two's complement binary representation
        ///     of the specified {@code int} value, or 32 if the value
        ///     is equal to zero.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NumberOfLeadingZeros(int i)
        {
            unchecked
            {
                // HD, Figure 5-6
                if (i == 0)
                {
                    return 32;
                }

                int n = 1;
                if ((int)((uint)i >> 16) == 0)
                {
                    n += 16;
                    i <<= 16;
                }

                if ((int)((uint)i >> 24) == 0)
                {
                    n += 8;
                    i <<= 8;
                }

                if ((int)((uint)i >> 28) == 0)
                {
                    n += 4;
                    i <<= 4;
                }

                if ((int)((uint)i >> 30) == 0)
                {
                    n += 2;
                    i <<= 2;
                }

                n -= (int)((uint)i >> 31);
                return n;
            }
        }
    }

    // ReSharper disable InconsistentNaming


    /// <summary>
    /// Miscellaneous useful functions for dealing with low level bits and bytes.
    /// </summary>
    public class BitUtil
        {
            /// <summary>
            /// Size of a byte in bytes
            /// </summary>
            public const int SIZE_OF_BYTE = 1;

            /// <summary>
            /// Size of a boolean in bytes
            /// </summary>
            public const int SIZE_OF_BOOLEAN = 1;

            /// <summary>
            /// Size of a char in bytes
            /// </summary>
            public const int SIZE_OF_CHAR = 2;

            /// <summary>
            /// Size of a short in bytes
            /// </summary>
            public const int SIZE_OF_SHORT = 2;

            /// <summary>
            /// Size of an int in bytes
            /// </summary>
            public const int SIZE_OF_INT = 4;

            /// <summary>
            /// Size of a a float in bytes
            /// </summary>
            public const int SIZE_OF_FLOAT = 4;

            /// <summary>
            /// Size of a long in bytes
            /// </summary>
            public const int SIZE_OF_LONG = 8;

            /// <summary>
            /// Size of a double in bytes
            /// </summary>
            public const int SIZE_OF_DOUBLE = 8;

            /// <summary>
            /// Length of the data blocks used by the CPU cache sub-system in bytes.
            /// </summary>
            public const int CACHE_LINE_LENGTH = 64;

            private static readonly byte[] HexDigitTable = {
            (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5', (byte) '6', (byte) '7',
            (byte) '8', (byte) '9', (byte) 'a', (byte) 'b', (byte) 'c', (byte) 'd', (byte) 'e', (byte) 'f'
        };

            private static readonly byte[] FromHexDigitTable;

            static BitUtil()
            {
                FromHexDigitTable = new byte[128];
                FromHexDigitTable['0'] = 0x00;
                FromHexDigitTable['1'] = 0x01;
                FromHexDigitTable['2'] = 0x02;
                FromHexDigitTable['3'] = 0x03;
                FromHexDigitTable['4'] = 0x04;
                FromHexDigitTable['5'] = 0x05;
                FromHexDigitTable['6'] = 0x06;
                FromHexDigitTable['7'] = 0x07;
                FromHexDigitTable['8'] = 0x08;
                FromHexDigitTable['9'] = 0x09;
                FromHexDigitTable['a'] = 0x0a;
                FromHexDigitTable['A'] = 0x0a;
                FromHexDigitTable['b'] = 0x0b;
                FromHexDigitTable['B'] = 0x0b;
                FromHexDigitTable['c'] = 0x0c;
                FromHexDigitTable['C'] = 0x0c;
                FromHexDigitTable['d'] = 0x0d;
                FromHexDigitTable['D'] = 0x0d;
                FromHexDigitTable['e'] = 0x0e;
                FromHexDigitTable['E'] = 0x0e;
                FromHexDigitTable['f'] = 0x0f;
                FromHexDigitTable['F'] = 0x0f;
            }

            private const int LastDigitMask = 1;

            private static readonly Encoding Utf8Encoding = Encoding.UTF8;

            /// <summary>
            /// Fast method of finding the next power of 2 greater than or equal to the supplied value.
            ///
            /// If the value is &lt;= 0 then 1 will be returned.
            ///
            /// This method is not suitable for <seealso cref="int.MinValue"/> or numbers greater than 2^30.
            /// </summary>
            /// <param name="value"> from which to search for next power of 2 </param>
            /// <returns> The next power of 2 or the value itself if it is a power of 2 </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int FindNextPositivePowerOfTwo(int value)
            {
                unchecked
                {
                    return 1 << (32 - IntUtil.NumberOfLeadingZeros(value - 1));
                }
            }

            /// <summary>
            /// Align a value to the next multiple up of alignment.
            /// If the value equals an alignment multiple then it is returned unchanged.
            /// <para>
            /// This method executes without branching. This code is designed to be use in the fast path and should not
            /// be used with negative numbers. Negative numbers will result in undefined behavior.
            ///
            /// </para>
            /// </summary>
            /// <param name="value">     to be aligned up. </param>
            /// <param name="alignment"> to be used. </param>
            /// <returns> the value aligned to the next boundary. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Align(int value, int alignment)
            {
                return (value + (alignment - 1)) & ~(alignment - 1);
            }

            /// <summary>
            /// Generate a byte array from the hex representation of the given byte array.
            /// </summary>
            /// <param name="buffer"> to convert from a hex representation (in Big Endian) </param>
            /// <returns> new byte array that is decimal representation of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] FromHexByteArray(byte[] buffer)
            {
                byte[] outputBuffer = new byte[buffer.Length >> 1];

                for (int i = 0; i < buffer.Length; i += 2)
                {
                    outputBuffer[i >> 1] = (byte)((FromHexDigitTable[buffer[i]] << 4) | FromHexDigitTable[buffer[i + 1]]);
                }

                return outputBuffer;
            }

            /// <summary>
            /// Generate a byte array that is a hex representation of a given byte array.
            /// </summary>
            /// <param name="buffer"> to convert to a hex representation </param>
            /// <returns> new byte array that is hex representation (in Big Endian) of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] ToHexByteArray(byte[] buffer)
            {
                return ToHexByteArray(buffer, 0, buffer.Length);
            }

            /// <summary>
            /// Generate a byte array that is a hex representation of a given byte array.
            /// </summary>
            /// <param name="buffer"> to convert to a hex representation </param>
            /// <param name="offset"> the offset into the buffer </param>
            /// <param name="length"> the number of bytes to convert </param>
            /// <returns> new byte array that is hex representation (in Big Endian) of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] ToHexByteArray(byte[] buffer, int offset, int length)
            {
                var outputBuffer = new byte[length << 1];

                for (var i = 0; i < (length << 1); i += 2)
                {
                    var b = buffer[offset + (i >> 1)];

                    outputBuffer[i] = HexDigitTable[(b >> 4) & 0x0F];
                    outputBuffer[i + 1] = HexDigitTable[b & 0x0F];
                }

                return outputBuffer;
            }

            /// <summary>
            /// Generate a byte array from a string that is the hex representation of the given byte array.
            /// </summary>
            /// <param name="value"> to convert from a hex representation (in Big Endian) </param>
            /// <returns> new byte array holding the decimal representation of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte[] FromHex(string value)
            {
                return FromHexByteArray(Utf8Encoding.GetBytes(value));
            }

            /// <summary>
            /// Generate a string that is the hex representation of a given byte array.
            /// </summary>
            /// <param name="buffer"> to convert to a hex representation </param>
            /// <param name="offset"> the offset into the buffer </param>
            /// <param name="length"> the number of bytes to convert </param>
            /// <returns> new String holding the hex representation (in Big Endian) of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToHex(byte[] buffer, int offset, int length)
            {
                var hexByteArray = ToHexByteArray(buffer, offset, length);
                return Utf8Encoding.GetString(hexByteArray, 0, hexByteArray.Length);
            }

            /// <summary>
            /// Generate a string that is the hex representation of a given byte array.
            /// </summary>
            /// <param name="buffer"> to convert to a hex representation </param>
            /// <returns> new String holding the hex representation (in Big Endian) of the passed array </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToHex(byte[] buffer)
            {
                var hexByteArray = ToHexByteArray(buffer);
                return Utf8Encoding.GetString(hexByteArray, 0, hexByteArray.Length);
            }

            /// <summary>
            /// Is a number even.
            /// </summary>
            /// <param name="value"> to check. </param>
            /// <returns> true if the number is even otherwise false. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsEven(int value)
            {
                return (value & LastDigitMask) == 0;
            }

            /// <summary>
            /// Is a value a positive power of two.
            /// </summary>
            /// <param name="value"> to be checked. </param>
            /// <returns> true if the number is a positive power of two otherwise false. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsPowerOfTwo(int value)
            {
                return value > 0 && ((value & (~value + 1)) == value);
            }

            /// <summary>
            /// Cycles indices of an array one at a time in a forward fashion
            /// </summary>
            /// <param name="current"> value to be incremented. </param>
            /// <param name="max">     value for the cycle. </param>
            /// <returns> the next value, or zero if max is reached. </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Next(int current, int max)
            {
                int next = current + 1;
                if (next == max)
                {
                    next = 0;
                }

                return next;
            }

            /// <summary>
            /// Cycles indices of an array one at a time in a backwards fashion
            /// </summary>
            /// <param name="current"> value to be decremented. </param>
            /// <param name="max">     value of the cycle. </param>
            /// <returns> the next value, or max - 1 if current is zero </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Previous(int current, int max)
            {
                if (0 == current)
                {
                    return max - 1;
                }

                return current - 1;
            }

            /// <summary>
            /// Is an address aligned on a boundary.
            /// </summary>
            /// <param name="address">   to be tested. </param>
            /// <param name="alignment"> boundary the address is tested against. </param>
            /// <returns> true if the address is on the aligned boundary otherwise false. </returns>
            /// <exception cref="ArgumentException"> if the alignment is not a power of 2` </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsAligned(long address, int alignment)
            {
                if (!IsPowerOfTwo(alignment))
                {
                    throw new ArgumentException("Alignment must be a power of 2: alignment=" + alignment);
                }

                return (address & (alignment - 1)) == 0;
            }
        }
    

    /// <summary>
    /// Manages pools of RecyclableMemoryStream objects.
    /// </summary>
    /// <remarks>
    /// There are two pools managed in here. The small pool contains same-sized buffers that are handed to streams
    /// as they write more data.
    ///
    /// For scenarios that need to call GetBuffer(), the large pool contains buffers of various sizes, all
    /// multiples of LargeBufferMultiple (1 MB by default). They are split by size to avoid overly-wasteful buffer
    /// usage. There should be far fewer 8 MB buffers than 1 MB buffers, for example.
    /// </remarks>
    public partial class RecyclableMemoryStreamManager
    {
        public static RecyclableMemoryStreamManager Default = new RecyclableMemoryStreamManager(4 * 1024, DefaultLargeBufferMultiple, DefaultMaximumBufferSize);

        /// <summary>
        /// Generic delegate for handling events without any arguments.
        /// </summary>
        public delegate void EventHandler();

        /// <summary>
        /// Delegate for handling large buffer discard reports.
        /// </summary>
        /// <param name="reason">Reason the buffer was discarded.</param>
        public delegate void LargeBufferDiscardedEventHandler(Events.MemoryStreamDiscardReason reason);

        /// <summary>
        /// Delegate for handling reports of stream size when streams are allocated
        /// </summary>
        /// <param name="bytes">Bytes allocated.</param>
        public delegate void StreamLengthReportHandler(long bytes);

        /// <summary>
        /// Delegate for handling periodic reporting of memory use statistics.
        /// </summary>
        /// <param name="smallPoolInUseBytes">Bytes currently in use in the small pool.</param>
        /// <param name="smallPoolFreeBytes">Bytes currently free in the small pool.</param>
        /// <param name="largePoolInUseBytes">Bytes currently in use in the large pool.</param>
        /// <param name="largePoolFreeBytes">Bytes currently free in the large pool.</param>
        public delegate void UsageReportEventHandler(
            long smallPoolInUseBytes, long smallPoolFreeBytes, long largePoolInUseBytes, long largePoolFreeBytes);

        // Spreads uses RMS mostly for small messages
        public const int DefaultBlockSize = 64 * 1024; // Max Pow2 < LOH

        public const int DefaultLargeBufferMultiple = 128 * 1024; // Min Pow2 > LOH
        public const int DefaultMaximumBufferSize = 10 * 1024 * 1024; // 10 MB

        private readonly int _blockSize;
        private readonly long[] _largeBufferFreeSize;
        private readonly long[] _largeBufferInUseSize;

        private readonly int _largeBufferMultiple;

        /// <summary>
        /// pools[0] = 1x largeBufferMultiple buffers
        /// pools[1] = 2x largeBufferMultiple buffers
        /// etc., up to maximumBufferSize
        /// </summary>
        private readonly ConcurrentStack<byte[]>[] _largePools;

        private readonly int _maximumBufferSize;

        /// <summary>
        /// Initializes the memory manager with the default block/buffer specifications.
        /// </summary>
        public RecyclableMemoryStreamManager()
            : this(DefaultBlockSize, DefaultLargeBufferMultiple, DefaultMaximumBufferSize) { }

        /// <summary>
        /// Initializes the memory manager with the given block requiredSize.
        /// </summary>
        /// <param name="blockSize">Size of each block that is pooled. Must be > 0. Will use the next power of two if the given size in not a power of two.</param>
        /// <param name="largeBufferMultiple">Each large buffer will be a multiple of this value.</param>
        /// <param name="maximumBufferSize">Buffers larger than this are not pooled</param>
        /// <exception cref="ArgumentOutOfRangeException">blockSize is not a positive number, or largeBufferMultiple is not a positive number, or maximumBufferSize is less than blockSize.</exception>
        /// <exception cref="ArgumentException">maximumBufferSize is not a multiple of largeBufferMultiple</exception>
        public RecyclableMemoryStreamManager(int blockSize, int largeBufferMultiple, int maximumBufferSize)
        {
            if (blockSize <= 0)
            {
                throw new ArgumentOutOfRangeException("blockSize", blockSize, "blockSize must be a positive number");
            }

            if (largeBufferMultiple <= 0)
            {
                throw new ArgumentOutOfRangeException("largeBufferMultiple",
                                                      "largeBufferMultiple must be a positive number");
            }

            if (maximumBufferSize < blockSize)
            {
                throw new ArgumentOutOfRangeException("maximumBufferSize",
                                                      "maximumBufferSize must be at least blockSize");
            }

            _blockSize = BitUtil.FindNextPositivePowerOfTwo(blockSize);
            _largeBufferMultiple = largeBufferMultiple;
            _maximumBufferSize = maximumBufferSize;

            if (!IsLargeBufferMultiple(maximumBufferSize))
            {
                throw new ArgumentException("maximumBufferSize is not a multiple of largeBufferMultiple",
                                            "maximumBufferSize");
            }

            //_smallPool = new ConcurrentBag<byte[]>();
            var numLargePools = maximumBufferSize / largeBufferMultiple;

            // +1 to store size of bytes in use that are too large to be pooled
            _largeBufferInUseSize = new long[numLargePools + 1];
            _largeBufferFreeSize = new long[numLargePools];

            _largePools = new ConcurrentStack<byte[]>[numLargePools];

            for (var i = 0; i < _largePools.Length; ++i)
            {
                _largePools[i] = new ConcurrentStack<byte[]>();
            }

            Events.Write.MemoryStreamManagerInitialized(blockSize, largeBufferMultiple, maximumBufferSize);
        }

        /// <summary>
        /// The size of each block. It must be set at creation and cannot be changed.
        /// </summary>
        public int BlockSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _blockSize; }
        }

        /// <summary>
        /// All buffers are multiples of this number. It must be set at creation and cannot be changed.
        /// </summary>
        public int LargeBufferMultiple
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _largeBufferMultiple; }
        }

        /// <summary>
        /// Gets or sets the maximum buffer size.
        /// </summary>
        /// <remarks>Any buffer that is returned to the pool that is larger than this will be
        /// discarded and garbage collected.</remarks>
        public int MaximumBufferSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _maximumBufferSize; }
        }

        /// <summary>
        /// Number of bytes in large pool not currently in use
        /// </summary>
        public long LargePoolFreeSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _largeBufferFreeSize.Sum(); }
        }

        /// <summary>
        /// Number of bytes currently in use by streams from the large pool
        /// </summary>
        public long LargePoolInUseSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _largeBufferInUseSize.Sum(); }
        }

        /// <summary>
        /// How many buffers are in the large pool
        /// </summary>
        public long LargeBuffersFree
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                long free = 0;
                foreach (var pool in _largePools)
                {
                    free += pool.Count;
                }
                return free;
            }
        }

        /// <summary>
        /// How many bytes of large free buffers to allow before we start dropping
        /// those returned to us.
        /// </summary>
        public long MaximumFreeLargePoolBytes { get; set; }

        /// <summary>
        /// Maximum stream capacity in bytes. Attempts to set a larger capacity will
        /// result in an exception.
        /// </summary>
        /// <remarks>A value of 0 indicates no limit.</remarks>
        public long MaximumStreamCapacity { get; set; }

        /// <summary>
        /// Whether to save callstacks for stream allocations. This can help in debugging.
        /// It should NEVER be turned on generally in production.
        /// </summary>
        public bool GenerateCallStacks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            set;
        }

        /// <summary>
        /// Whether dirty buffers can be immediately returned to the buffer pool. E.g. when GetBuffer() is called on
        /// a stream and creates a single large buffer, if this setting is enabled, the other blocks will be returned
        /// to the buffer pool immediately.
        /// Note when enabling this setting that the user is responsible for ensuring that any buffer previously
        /// retrieved from a stream which is subsequently modified is not used after modification (as it may no longer
        /// be valid).
        /// </summary>
        public bool AggressiveBufferReturn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            set;
        } = true;

        /// <summary>
        /// Removes and returns a single block from the pool.
        /// </summary>
        /// <returns>A byte[] array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte[] GetBlock()
        {
            var buffer = BufferPool<byte>.Rent(_blockSize);
            if (buffer.Length == _blockSize) return buffer;
            BufferPool<byte>.Return(buffer, false);
            buffer = new byte[_blockSize];
            return buffer;
        }

        /// <summary>
        /// Returns a buffer of arbitrary size from the large buffer pool. This buffer
        /// will be at least the requiredSize and always be a multiple of largeBufferMultiple.
        /// </summary>
        /// <param name="requiredSize">The minimum length of the buffer</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <returns>A buffer of at least the required size.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal byte[] GetLargeBuffer(int requiredSize, string tag)
        {
            requiredSize = RoundToLargeBufferMultiple(requiredSize);

            var poolIndex = requiredSize / _largeBufferMultiple - 1;

            byte[] buffer;
            if (poolIndex < _largePools.Length)
            {
                if (!_largePools[poolIndex].TryPop(out buffer))
                {
                    buffer = new byte[requiredSize];

                    Events.Write.MemoryStreamNewLargeBufferCreated(requiredSize, LargePoolInUseSize);
                    ReportLargeBufferCreated();
                }
                else
                {
                    Interlocked.Add(ref _largeBufferFreeSize[poolIndex], -buffer.Length);
                }
            }
            else
            {
                // Memory is too large to pool. They get a new buffer.

                // We still want to track the size, though, and we've reserved a slot
                // in the end of the inuse array for nonpooled bytes in use.
                poolIndex = _largeBufferInUseSize.Length - 1;

                // We still want to round up to reduce heap fragmentation.
                buffer = new byte[requiredSize];
                string callStack = null;
                if (GenerateCallStacks)
                {
                    // Grab the stack -- we want to know who requires such large buffers
                    callStack = Environment.StackTrace;
                }
                Events.Write.MemoryStreamNonPooledLargeBufferCreated(requiredSize, tag, callStack);
                ReportLargeBufferCreated();
            }

            Interlocked.Add(ref _largeBufferInUseSize[poolIndex], buffer.Length);

            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RoundToLargeBufferMultiple(int requiredSize)
        {
            return ((requiredSize + LargeBufferMultiple - 1) / LargeBufferMultiple) * LargeBufferMultiple;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLargeBufferMultiple(int value)
        {
            return (value != 0) && (value % LargeBufferMultiple) == 0;
        }

        /// <summary>
        /// Returns the buffer to the large pool
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        /// <param name="tag">The tag of the stream returning this buffer, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">buffer is null</exception>
        /// <exception cref="ArgumentException">buffer.Length is not a multiple of LargeBufferMultiple (it did not originate from this pool)</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReturnLargeBuffer(byte[] buffer, string tag)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!IsLargeBufferMultiple(buffer.Length))
            {
                BufferPool<byte>.Return(buffer);
                return;
            }

            var poolIndex = buffer.Length / _largeBufferMultiple - 1;

            if (poolIndex < _largePools.Length)
            {
                if ((_largePools[poolIndex].Count + 1) * buffer.Length <= MaximumFreeLargePoolBytes ||
                    MaximumFreeLargePoolBytes == 0)
                {
                    _largePools[poolIndex].Push(buffer);
                    Interlocked.Add(ref _largeBufferFreeSize[poolIndex], buffer.Length);
                }
                else
                {
                    Events.Write.MemoryStreamDiscardBuffer(Events.MemoryStreamBufferType.Large, tag,
                                                           Events.MemoryStreamDiscardReason.EnoughFree);
                    ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason.EnoughFree);
                }
            }
            else
            {
                // This is a non-poolable buffer, but we still want to track its size for inuse
                // analysis. We have space in the inuse array for this.
                poolIndex = _largeBufferInUseSize.Length - 1;

                Events.Write.MemoryStreamDiscardBuffer(Events.MemoryStreamBufferType.Large, tag,
                                                       Events.MemoryStreamDiscardReason.TooLarge);
                ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason.TooLarge);
            }

            Interlocked.Add(ref _largeBufferInUseSize[poolIndex], -buffer.Length);

            //ReportUsageReport(_smallPoolInUseSize, _smallPoolFreeSize, LargePoolInUseSize, LargePoolFreeSize);
            ReportUsageReport(0, 0, LargePoolInUseSize, LargePoolFreeSize);
        }

        /// <summary>
        /// Returns the blocks to the pool
        /// </summary>
        /// <param name="blocks">Collection of blocks to return to the pool</param>
        /// <param name="tag">The tag of the stream returning these blocks, for logging if necessary.</param>
        /// <exception cref="ArgumentNullException">blocks is null</exception>
        /// <exception cref="ArgumentException">blocks contains buffers that are the wrong size (or null) for this memory manager</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReturnBlocks(List<byte[]> blocks, string tag)
        {
            if (blocks == null)
            {
                throw new ArgumentNullException(nameof(blocks));
            }

            if (blocks.Count > 0)
            {
                foreach (var block in blocks)
                {
                    if (block == null || block.Length != BlockSize)
                    {
                        throw new ArgumentException("blocks contains buffers that are not BlockSize in length");
                    }

                    BufferPool<byte>.Return(block, false);
                }
            }

            ReportUsageReport(0, 0, LargePoolInUseSize, LargePoolFreeSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportBlockCreated()
        {
            var blockCreated = Interlocked.CompareExchange(ref BlockCreated, null, null);
            blockCreated?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportBlockDiscarded()
        {
            var blockDiscarded = Interlocked.CompareExchange(ref BlockDiscarded, null, null);
            blockDiscarded?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportLargeBufferCreated()
        {
            var largeBufferCreated = Interlocked.CompareExchange(ref LargeBufferCreated, null, null);
            largeBufferCreated?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportLargeBufferDiscarded(Events.MemoryStreamDiscardReason reason)
        {
            var largeBufferDiscarded = Interlocked.CompareExchange(ref LargeBufferDiscarded, null, null);
            largeBufferDiscarded?.Invoke(reason);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportStreamCreated()
        {
            var streamCreated = Interlocked.CompareExchange(ref StreamCreated, null, null);
            streamCreated?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportStreamDisposed()
        {
            var streamDisposed = Interlocked.CompareExchange(ref StreamDisposed, null, null);
            streamDisposed?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportStreamFinalized()
        {
            var streamFinalized = Interlocked.CompareExchange(ref StreamFinalized, null, null);
            streamFinalized?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportStreamLength(long bytes)
        {
            var streamLength = Interlocked.CompareExchange(ref StreamLength, null, null);
            streamLength?.Invoke(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportStreamToArray()
        {
            var streamConvertedToArray = Interlocked.CompareExchange(ref StreamConvertedToArray, null, null);
            streamConvertedToArray?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("TRACE_RMS")]
        internal void ReportUsageReport(
            long smallPoolInUseBytes, long smallPoolFreeBytes, long largePoolInUseBytes, long largePoolFreeBytes)
        {
            var usageReport = Interlocked.CompareExchange(ref UsageReport, null, null);
            usageReport?.Invoke(smallPoolInUseBytes, smallPoolFreeBytes, largePoolInUseBytes, largePoolFreeBytes);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with no tag and a default initial capacity.
        /// </summary>
        /// <returns>A MemoryStream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecyclableMemoryStream GetStream()
        {
            return RecyclableMemoryStream.Create(this);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and a default initial capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <returns>A MemoryStream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecyclableMemoryStream GetStream(string tag)
        {
            return RecyclableMemoryStream.Create(this, tag);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity.
        /// </summary>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <returns>A MemoryStream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecyclableMemoryStream GetStream(string tag, int requiredSize)
        {
            return RecyclableMemoryStream.Create(this, tag, requiredSize);
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and at least the given capacity, possibly using
        /// a single continugous underlying buffer.
        /// </summary>
        /// <remarks>Retrieving a MemoryStream which provides a single contiguous buffer can be useful in situations
        /// where the initial size is known and it is desirable to avoid copying data between the smaller underlying
        /// buffers to a single large one. This is most helpful when you know that you will always call GetBuffer
        /// on the underlying stream.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="requiredSize">The minimum desired capacity for the stream.</param>
        /// <param name="asContiguousBuffer">Whether to attempt to use a single contiguous buffer.</param>
        /// <returns>A MemoryStream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RecyclableMemoryStream GetStream(string tag, int requiredSize, bool asContiguousBuffer)
        {
            if (!asContiguousBuffer || requiredSize <= BlockSize)
            {
                return GetStream(tag, requiredSize);
            }

            return RecyclableMemoryStream.Create(this, tag, requiredSize, GetLargeBuffer(requiredSize, tag));
        }

        /// <summary>
        /// Retrieve a new MemoryStream object with the given tag and with contents copied from the provided
        /// buffer. The provided buffer is not wrapped or used after construction.
        /// </summary>
        /// <remarks>The new stream's position is set to the beginning of the stream when returned.</remarks>
        /// <param name="tag">A tag which can be used to track the source of the stream.</param>
        /// <param name="buffer">The byte buffer to copy data from.</param>
        /// <param name="offset">The offset from the start of the buffer to copy from.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <returns>A MemoryStream.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public RecyclableMemoryStream GetStream(string tag, byte[] buffer, int offset, int count)
        {
            var stream = RecyclableMemoryStream.Create(this, tag, count);
            stream.Write(buffer, offset, count);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockCreated;

        /// <summary>
        /// Triggered when a new block is created.
        /// </summary>
        public event EventHandler BlockDiscarded;

        /// <summary>
        /// Triggered when a new large buffer is created.
        /// </summary>
        public event EventHandler LargeBufferCreated;

        /// <summary>
        /// Triggered when a new stream is created.
        /// </summary>
        public event EventHandler StreamCreated;

        /// <summary>
        /// Triggered when a stream is disposed.
        /// </summary>
        public event EventHandler StreamDisposed;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event EventHandler StreamFinalized;

        /// <summary>
        /// Triggered when a stream is finalized.
        /// </summary>
        public event StreamLengthReportHandler StreamLength;

        /// <summary>
        /// Triggered when a user converts a stream to array.
        /// </summary>
        public event EventHandler StreamConvertedToArray;

        /// <summary>
        /// Triggered when a large buffer is discarded, along with the reason for the discard.
        /// </summary>
        public event LargeBufferDiscardedEventHandler LargeBufferDiscarded;

        /// <summary>
        /// Periodically triggered to report usage statistics.
        /// </summary>
        public event UsageReportEventHandler UsageReport;
    }
}
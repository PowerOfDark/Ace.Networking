﻿// https://github.com/AdaptiveConsulting/Aeron.NET/blob/master/src/Adaptive.Agrona/Util/IntUtil.cs

using System.Runtime.CompilerServices;

namespace Spreads.Utils
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
}
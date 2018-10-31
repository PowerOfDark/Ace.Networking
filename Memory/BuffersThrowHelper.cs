// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Spreads.Buffers
{
    /// <summary>
    /// Buffers throw helper
    /// </summary>
    internal static class BuffersThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBadLength()
        {
            throw new ArgumentOutOfRangeException("length");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNull<T>()
        {
            throw new ArgumentNullException(nameof(T));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDisposed<T>()
        {
            throw new ObjectDisposedException(nameof(T));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDisposingRetained<T>()
        {
            throw new InvalidOperationException("Cannot dispose retained " + nameof(T));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThowNegativeRefCount()
        {
            throw new InvalidOperationException("Negative ref count");
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //internal static void ThrowAlienOrAlreadyPooled<T>()
        //{
        //    throw new InvalidOperationException("Cannot return to pool alien or already pooled " + nameof(T));
        //}

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAlreadyPooled<T>()
        {
            throw new InvalidOperationException("Cannot return to a pool an already pooled " + nameof(T));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotFromPool<T>()
        {
            throw new InvalidOperationException("Memory not from pool " + nameof(T));
        }
    }
}
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using Spreads.Buffers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ace.Networking.Memory;
using static System.Runtime.CompilerServices.Unsafe;

#pragma warning disable HAA0101 // Array allocation for params parameter

namespace Spreads.Serialization
{
    internal delegate int FromPtrDelegate(IntPtr ptr, out object value);

    internal delegate int ToPtrDelegate(object value, IntPtr destination, MemoryStream ms = null, SerializationFormat compression = SerializationFormat.Binary);

    internal delegate int SizeOfDelegate(object value, out MemoryStream memoryStream, SerializationFormat compression = SerializationFormat.Binary);

    public static class TypeHelper
    {
        private static readonly Dictionary<Type, FromPtrDelegate> FromPtrDelegateCache = new Dictionary<Type, FromPtrDelegate>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FromPtrDelegate GetFromPtrDelegate(Type ty)
        {
            FromPtrDelegate temp;
            if (FromPtrDelegateCache.TryGetValue(ty, out temp)) return temp;
            var mi = typeof(TypeHelper).GetMethod("ReadObject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException

            var genericMi = mi.MakeGenericMethod(ty);

            temp = (FromPtrDelegate)genericMi.CreateDelegate(typeof(FromPtrDelegate));
            FromPtrDelegateCache[ty] = temp;
            return temp;
        }

        private static readonly Dictionary<Type, ToPtrDelegate> ToPtrDelegateCache = new Dictionary<Type, ToPtrDelegate>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ToPtrDelegate GetToPtrDelegate(Type ty)
        {
            ToPtrDelegate temp;
            if (ToPtrDelegateCache.TryGetValue(ty, out temp)) return temp;
            var mi = typeof(TypeHelper).GetMethod("WriteObject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            var genericMi = mi.MakeGenericMethod(ty);
            temp = (ToPtrDelegate)genericMi.CreateDelegate(typeof(ToPtrDelegate));
            ToPtrDelegateCache[ty] = temp;
            return temp;
        }

        private static readonly Dictionary<Type, SizeOfDelegate> SizeOfDelegateCache = new Dictionary<Type, SizeOfDelegate>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SizeOfDelegate GetSizeOfDelegate(Type ty)
        {
            SizeOfDelegate temp;
            if (SizeOfDelegateCache.TryGetValue(ty, out temp)) return temp;
            var mi = typeof(TypeHelper).GetMethod("SizeOfObject", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            var genericMi = mi.MakeGenericMethod(ty);
            temp = (SizeOfDelegate)genericMi.CreateDelegate(typeof(SizeOfDelegate));
            SizeOfDelegateCache[ty] = temp;
            return temp;
        }

        private static readonly Dictionary<Type, int> SizeDelegateCache = new Dictionary<Type, int>();

        // used by reflection below
        // ReSharper disable once UnusedMember.Local
        private static int Size<T>()
        {
            return TypeHelper<T>.FixedSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetSize(Type ty)
        {
            int temp;
            if (SizeDelegateCache.TryGetValue(ty, out temp)) return temp;
            var mi = typeof(TypeHelper).GetMethod("Size", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            // ReSharper disable once PossibleNullReferenceException
            var genericMi = mi.MakeGenericMethod(ty);
            temp = (int)genericMi.Invoke(null, new object[] { });
            SizeDelegateCache[ty] = temp;
            return temp;
        }
    }

    // NB Optimization fail: static RO are not JIT consts for generics. Although for tiered compilation this could work at some point.

    public static unsafe class TypeHelper<T>
    {
        // Just in case, do not use static ctor in any critical paths: https://github.com/Spreads/Spreads/issues/66
        // static TypeHelper() { }



        /// <summary>
        /// Returns a positive size of a pinnable type T, -1 if the type T is not pinnable or has
        /// a registered <see cref="IBinaryConverter{T}"/> converter.
        /// We assume the type T is pinnable if `GCHandle.Alloc(T[2], GCHandleType.Pinned) = true`.
        /// This is more relaxed than Marshal.SizeOf, but still doesn't cover cases such as
        /// an array of KVP[DateTime,double], which has contiguous layout in memory.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int FixedSize = InitChecked();


        // NOTE: PinnedSize simply tries to pin via GCHandle.
        // FixedSize could be positive for non-pinnable structs with auto layout (e.g. DateTime)
        // but it is opt-in and requires an attribute to treat a custom blittable type as fixed.

        /// <summary>
        /// True if an array T[] could be pinned in memory.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        public static readonly bool IsPinnable = PinnedSize() > 0 || FixedSize > 0;


        /// <summary>
        /// CLR definition, we cache it here since ty.IsValueType is a virtual call
        /// </summary>
        public static readonly bool IsValueType = typeof(T).GetTypeInfo().IsValueType;

        /// <summary>
        /// Implements <see cref="IDelta{T}"/>
        /// </summary>
        public static readonly bool IsIDelta = typeof(IDelta<T>).GetTypeInfo().IsAssignableFrom(typeof(T));

        

        // ReSharper disable once StaticMemberInGenericType
        public static readonly bool IsFixedSize = FixedSize > 0;



        private static int InitChecked()
        {
            try
            {
                var size = Init();
                if (size > 255)
                {
                    size = -1;
                }

                // NB do not support huge blittable type
                return size;
            }
            catch
            {
                return -1;
            }
        }

        private static int PinnedSize()
        {
            try
            {
                var array = new T[2];
                var pinnedArrayHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                var size = (int)
                    (Marshal.UnsafeAddrOfPinnedArrayElement(array, 1).ToInt64() -
                     Marshal.UnsafeAddrOfPinnedArrayElement(array, 0).ToInt64());
                pinnedArrayHandle.Free();
                // Type helper works only with types that could be pinned in arrays
                // Here we just cross-check, happens only in static constructor
                var unsafeSize = SizeOf<T>();
                //if (unsafeSize != size) { ThrowHelper.FailFast("Pinned and unsafe sizes differ!"); }
                return size;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Method is only called from the static constructor of TypeHelper.
        /// </summary>
        /// <returns></returns>
        private static int Init()
        {
            if (typeof(T) == typeof(DateTime))
            {
                return 8;
            }
            // NB decimal is pinnable but not primitive, the check below fails on it
            if (typeof(T) == typeof(decimal))
            {
                return 16;
            }

            var pinnedSize = PinnedSize();

            if (pinnedSize > 0)
            {
                if (typeof(T).GetTypeInfo().IsPrimitive)
                {
                    return pinnedSize;
                }

                // for a non-primitive type to be blittable, it must have an attribute

                var hasSizeAttribute = false;

                if (hasSizeAttribute)
                {
                    if (false /*typeof(IBinaryConverter<T>).IsAssignableFrom(typeof(T))*/)
                    {
                        // NB: this makes no sense, because blittable is version 0, if we have any change
                        // to struct layout later, we won't be able to work with version 0 anymore
                        // and will lose ability to work with old values.
                        Environment.FailFast($"Blittable types must not implement IBinaryConverter<T> interface.");
                    }
                    return pinnedSize;
                }
            }

            // by this line the type is not blittable

            //IBinaryConverter<T> converter = null;

            // NB we try to check interface as a last step, because some generic types
            // could implement IBinaryConverter<T> but still be blittable for certain types,
            // e.g. DateTime vs long in PersistentMap<K,V>.Entry
            //if (tmp is IBinaryConverter<T>) {


            // NB: string as UTF8 Json is OK
            // /else if (typeof(T) == typeof(string))
            // /{
            // /    BinaryConverter = (IBinaryConverter<T>)(new StringBinaryConverter());
            // /}

            // TODO
            if (typeof(T).IsArray)
            {
                var elementType = typeof(T).GetElementType();
                var elementSize = TypeHelper.GetSize(elementType);
                if (elementSize > 0)
                { // only for blittable types
                    //converter = (IBinaryConverter<T>)ArrayConverterFactory.Create(elementType);
                }
            }

            // Do not add Json converter as fallback, it is not "binary", it implements the interface for
            // simpler implementation in BinarySerializer and fallback happens there


            return -1;
        }

    }
}
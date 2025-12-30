#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

using System;

namespace S1API.Utils
{
    /// <summary>
    /// Extensions for Arrays.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Add's an item to an existing array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        public static T[] AddItemToArray<T>(this T[]? array, T item) =>
            Internal.Utils.ArrayExtensions.AddItemToArray(array, item);

#if (IL2CPPMELON || IL2CPPBEPINEX)
        /// <summary>
        /// Add's an item to an existing <see cref="Il2CppReferenceArray{T}"/> instance.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="itemsToAdd"></param>
        /// <typeparam name="T"></typeparam>
        public static Il2CppReferenceArray<T> AddItemToArray<T>(this Il2CppReferenceArray<T>? array, params T[]? itemsToAdd) where T : Il2CppSystem.Object =>
            Internal.Utils.ArrayExtensions.AddItemToArray(array, itemsToAdd);
#endif
    }
}


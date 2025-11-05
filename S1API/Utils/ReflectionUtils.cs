using System;
using System.Reflection;

namespace S1API.Utils
{
    /// <summary>
    /// Provides reflection-based utility methods for mod developers.
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Recursively searches for a method by name from a class down to the object type.
        /// </summary>
        /// <param name="type">The type you want to recursively search.</param>
        /// <param name="methodName">The name of the method you're searching for.</param>
        /// <param name="bindingFlags">The binding flags to apply during the search.</param>
        /// <returns>The method info if found, otherwise null.</returns>
        public static MethodInfo? GetMethod(Type? type, string methodName, BindingFlags bindingFlags) =>
            Internal.Utils.ReflectionUtils.GetMethod(type, methodName, bindingFlags);

        /// <summary>
        /// Checks whether the object is a ValueTuple.
        /// </summary>
        /// <param name="obj">The object type to check.</param>
        /// <returns>Whether the type is a ValueTuple or not.</returns>
        public static bool IsValueTuple(this object obj) =>
            Internal.Utils.ReflectionUtils.IsValueTuple(obj);

        /// <summary>
        /// Retrieves the items from the ValueTuple instance.
        /// </summary>
        /// <param name="obj">The ValueTuple instance.</param>
        /// <returns>The items in the ValueTuple instance.</returns>
        public static object[]? GetValueTupleItems(this object obj) =>
            Internal.Utils.ReflectionUtils.GetValueTupleItems(obj);
    }
}


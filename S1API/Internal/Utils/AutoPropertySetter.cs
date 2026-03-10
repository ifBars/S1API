using System;
using System.Reflection;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Helper for setting auto-properties that may have non-public setters.
    /// Tries the property setter first, then falls back to the compiler backing field (e.g., "&lt;PropName&gt;k__BackingField").
    /// </summary>
    internal static class AutoPropertySetter
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool TryInvokeSetter(MethodInfo setter, object target, object value)
        {
            try
            {
                setter.Invoke(target, new[] { value });
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TrySet(object target, string propertyName, object value)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return false;

            var type = target.GetType();
            var property = type.GetProperty(propertyName, InstanceFlags);
            var setter = property?.GetSetMethod(nonPublic: true);
            if (setter != null && TryInvokeSetter(setter, target, value))
                return true;

            try
            {
                return ReflectionUtils.TrySetFieldOrProperty(target, $"<{propertyName}>k__BackingField", value);
            }
            catch
            {
                return false;
            }
        }
    }
}


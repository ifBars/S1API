using S1API.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Provides generic reflection based methods for easier API development
    /// </summary>
    internal static class ReflectionUtils
    {
        /// <summary>
        /// Identifies all classes derived from another class.
        /// </summary>
        /// <typeparam name="TBaseClass">The base class derived from.</typeparam>
        /// <returns>A list of all types derived from the base class.</returns>
        internal static List<Type> GetDerivedClasses<TBaseClass>()
        {
            List<Type> derivedClasses = new List<Type>();
            Assembly[] applicableAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !ShouldSkipAssembly(assembly))
                .ToArray();
            foreach (Assembly assembly in applicableAssemblies)
                foreach (Type type in SafeGetTypes(assembly))
                {
                    try
                    {
                        if (type == null)
                            continue;
                        if (typeof(TBaseClass).IsAssignableFrom(type)
                            && type != typeof(TBaseClass)
                            && !type.IsAbstract)
                        {
                            derivedClasses.Add(type);
                        }
                    }
                    catch (TypeLoadException)
                    {
                        // Ideally, we'd log this, but can be noisy and we've got no logger elsewhere
                        continue;
                    }
                    catch (Exception)
                    {
                        // Catch-all for anything else (e.g., MissingMethodException)
                        continue;
                    }
                }
            return derivedClasses;
        }

        /// <summary>
        /// INTERNAL: Gets all types by their name.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>The actual type identified by the name.</returns>
        internal static Type? GetTypeByName(string typeName)
        {
            // Fast path: allow fully-qualified type names to resolve quickly
            try
            {
                var direct = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (direct != null)
                    return direct;
            }
            catch { /* ignore */ }

            // First search through likely candidate assemblies (skip core/system ones)
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies.Where(a => !ShouldSkipAssembly(a)))
            {
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type == null)
                        continue;

                    if (type.Name == typeName || type.FullName == typeName)
                        return type;
                }
            }

            // Fallback: search all assemblies but still use SafeGetTypes
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type == null)
                        continue;

                    if (type.Name == typeName || type.FullName == typeName || (type.FullName != null && type.FullName.EndsWith("." + typeName)))
                        return type;
                }
            }

            return null;
        }
        
        /// <summary>
        /// INTERNAL: Determines whether to skip an assembly during reflection searches. Will skip assemblies that are unlikely to contain mod/game types.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>Whether to skip the assembly or not.</returns>
        internal static bool ShouldSkipAssembly(Assembly assembly)
        {
            string? fullName = assembly.FullName;
            if (string.IsNullOrEmpty(fullName))
                return false;

            return fullName.StartsWith("System")
                   || fullName.StartsWith("Unity")
                   || fullName.StartsWith("Il2Cpp")
                   || fullName.StartsWith("mscorlib")
                   || fullName.StartsWith("Mono.")
                   || fullName.StartsWith("netstandard")
                   || fullName.StartsWith("com.rlabrecque")
                   || fullName.StartsWith("__Generated");
        }
        
        /// <summary>
        /// INTERNAL: Safely gets types from an assembly, even if some types fail to load.
        /// </summary>
        /// <param name="asm">The assembly to get types from.</param>
        /// <returns>The types that were successfully loaded from the assembly.</returns>
        internal static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!.Cast<Type>();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// INTERNAL: Recursively gets fields from a class down to the object type.
        /// </summary>
        /// <param name="type">The type you want to recursively search.</param>
        /// <param name="bindingFlags">The binding flags to apply during the search.</param>
        /// <returns></returns>
        internal static FieldInfo[] GetAllFields(Type? type, BindingFlags bindingFlags)
        {
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            while (type != null && type != typeof(object))
            {
                fieldInfos.AddRange(type.GetFields(bindingFlags));
                type = type.BaseType;
            }
            return fieldInfos.ToArray();
        }

        /// <summary>
        /// INTERNAL: Recursively searches for a method by name from a class down to the object type.
        /// </summary>
        /// <param name="type">The type you want to recursively search.</param>
        /// <param name="methodName">The name of the method you're searching for.</param>
        /// <param name="bindingFlags">The binding flags to apply during the search.</param>
        /// <returns></returns>
        public static MethodInfo? GetMethod(Type? type, string methodName, BindingFlags bindingFlags)
        {
            while (type != null && type != typeof(object))
            {
                MethodInfo? method = type.GetMethod(methodName, bindingFlags);
                if (method != null)
                    return method;

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// INTERNAL: The different ValueTuple types.
        /// </summary>
        private static readonly HashSet<Type> _valueTupleTypes = new HashSet<Type>()
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        };

        /// <summary>
        /// INTERNAL: Checks whether the object is a ValueTuple
        /// </summary>
        /// <param name="obj">The object type to check</param>
        /// <returns>Whether the type is a ValueTuple or not</returns>
        public static bool IsValueTuple(this object obj)
        {
            if (obj == null)
                return false;

            var type = obj.GetType();
            if (!type.IsValueType || !type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();
            return _valueTupleTypes.Contains(genericType);
        }

        /// <summary>
        /// INTERNAL: Retrieves the Items from the ValueTuple instance.
        /// </summary>
        /// <param name="obj">The ValueTuple instance</param>
        /// <returns>The items in the ValueTuple instance.</returns>
        public static object[]? GetValueTupleItems(this object obj)
        {
            if (!obj.IsValueTuple())
                return null;

            var fields = GetAllFields(obj.GetType(), BindingFlags.Instance | BindingFlags.Public);
            if (fields == null || fields.Length == 0)
                return null;

            return fields.Select(f => f.GetValue(obj))
                .ToArray();
        }

        /// <summary>
        /// INTERNAL: Shared cache for const string field retrieval across appearance classes.
        /// </summary>
        private static readonly Dictionary<Type, List<string>> _constStringFieldsCache = new Dictionary<Type, List<string>>();

        /// <summary>
        /// INTERNAL: Retrieves and caches all public <c>const string</c> fields defined in the specified type.
        /// Shared implementation used by appearance classes to avoid code duplication.
        /// </summary>
        /// <param name="type">The type from which to retrieve constant string fields.</param>
        /// <returns>
        /// A list of constant string values defined in the type.
        /// </returns>
        /// <remarks>
        /// Uses reflection to gather constants and caches them for future calls to improve performance.
        /// </remarks>
        internal static List<string> GetConstStringFields(Type type)
        {
            if (type == null)
                return new List<string>();

            if (_constStringFieldsCache.TryGetValue(type, out var cached))
                return cached;

            var consts = new List<string>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                    consts.Add((string)field.GetRawConstantValue());
            }

            _constStringFieldsCache[type] = consts;
            return consts;
        }

        /// <summary>
        /// INTERNAL: Attempts to set a field or property on an object using reflection.
        /// Tries field first, then property. Handles both public and non-public members.
        /// </summary>
        /// <param name="target">The target object to set the member on.</param>
        /// <param name="memberName">The name of the field or property.</param>
        /// <param name="value">The value to set.</param>
        /// <returns><c>true</c> if the member was successfully set; otherwise, <c>false</c>.</returns>
        internal static bool TrySetFieldOrProperty(object target, string memberName, object value)
        {
            if (target == null) return false;
            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Try field first
            var fi = type.GetField(memberName, flags);
            if (fi != null)
            {
                try
                {
                    if (value == null || fi.FieldType.IsInstanceOfType(value))
                    {
                        fi.SetValue(target, value);
                        return true;
                    }
                }
                catch { }
            }
            
            // Try property
            var pi = type.GetProperty(memberName, flags);
            if (pi != null && pi.CanWrite)
            {
                try
                {
                    if (value == null || pi.PropertyType.IsInstanceOfType(value))
                    {
                        pi.SetValue(target, value);
                        return true;
                    }
                }
                catch { }
            }
            
            return false;
        }

        /// <summary>
        /// INTERNAL: Attempts to get a field or property value from an object using reflection.
        /// Tries field first, then property. Handles both public and non-public members.
        /// </summary>
        /// <param name="target">The target object to get the member from.</param>
        /// <param name="memberName">The name of the field or property.</param>
        /// <returns>The value of the member, or <c>null</c> if not found or inaccessible.</returns>
        internal static object TryGetFieldOrProperty(object target, string memberName)
        {
            if (target == null) return null;
            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Try field first
            var fi = type.GetField(memberName, flags);
            if (fi != null)
            {
                try
                {
                    return fi.GetValue(target);
                }
                catch { }
            }
            
            // Try property
            var pi = type.GetProperty(memberName, flags);
            if (pi != null && pi.CanRead)
            {
                try
                {
                    return pi.GetValue(target);
                }
                catch { }
            }
            
            return null;
        }
    }
}

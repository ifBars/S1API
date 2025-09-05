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
                derivedClasses.AddRange(SafeGetTypes(assembly)
                    .Where(type => typeof(TBaseClass).IsAssignableFrom(type)
                                   && type != typeof(TBaseClass)
                                   && !type.IsAbstract));

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
    }
}

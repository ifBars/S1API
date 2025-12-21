using System.Reflection;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Base class for assembly analysis operations.
/// Provides common functionality for inspecting assemblies.
/// </summary>
public abstract class AssemblyAnalyzer
{
    protected Assembly Assembly { get; }
    protected string AssemblyPath { get; }
    
    protected AssemblyAnalyzer(Assembly assembly, string assemblyPath)
    {
        Assembly = assembly;
        AssemblyPath = assemblyPath;
    }
    
    /// <summary>
    /// Get all public types from the assembly.
    /// </summary>
    protected IEnumerable<Type> GetPublicTypes()
    {
        try
        {
            return Assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return the types that were successfully loaded
            return ex.Types.Where(t => t != null)!;
        }
    }
    
    /// <summary>
    /// Get all types from the assembly (including non-public).
    /// </summary>
    protected IEnumerable<Type> GetAllTypes()
    {
        try
        {
            return Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
    
    /// <summary>
    /// Get public fields from a type.
    /// </summary>
    protected static IEnumerable<FieldInfo> GetPublicFields(Type type)
    {
        try
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }
        catch
        {
            return Enumerable.Empty<FieldInfo>();
        }
    }
    
    /// <summary>
    /// Get all fields from a type (including non-public).
    /// </summary>
    protected static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        try
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
        catch
        {
            return Enumerable.Empty<FieldInfo>();
        }
    }
    
    /// <summary>
    /// Get public properties from a type.
    /// </summary>
    protected static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
    {
        try
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }
        catch
        {
            return Enumerable.Empty<PropertyInfo>();
        }
    }
    
    /// <summary>
    /// Get all properties from a type (including non-public).
    /// </summary>
    protected static IEnumerable<PropertyInfo> GetAllProperties(Type type)
    {
        try
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
        catch
        {
            return Enumerable.Empty<PropertyInfo>();
        }
    }
    
    /// <summary>
    /// Get public methods from a type (excluding property accessors and inherited Object methods).
    /// </summary>
    protected static IEnumerable<MethodInfo> GetPublicMethods(Type type)
    {
        try
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName); // Exclude property getters/setters
        }
        catch
        {
            return Enumerable.Empty<MethodInfo>();
        }
    }
    
    /// <summary>
    /// Get public events from a type.
    /// </summary>
    protected static IEnumerable<EventInfo> GetPublicEvents(Type type)
    {
        try
        {
            return type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }
        catch
        {
            return Enumerable.Empty<EventInfo>();
        }
    }
    
    /// <summary>
    /// Get a safe type name that handles generic types.
    /// </summary>
    protected static string GetSafeTypeName(Type? type)
    {
        if (type == null) return "void";
        
        try
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition().Name;
                var tickIndex = genericDef.IndexOf('`');
                if (tickIndex > 0)
                    genericDef = genericDef[..tickIndex];
                
                var args = type.GetGenericArguments()
                    .Select(GetSafeTypeName);
                return $"{genericDef}<{string.Join(", ", args)}>";
            }
            
            return type.Name;
        }
        catch
        {
            return type.Name;
        }
    }
    
    /// <summary>
    /// Check if a type is from the ScheduleOne namespace.
    /// </summary>
    protected static bool IsScheduleOneType(Type? type)
    {
        return type?.Namespace?.StartsWith("ScheduleOne.", StringComparison.Ordinal) == true
            || type?.Namespace == "ScheduleOne";
    }
    
    /// <summary>
    /// Check if a type is from the Il2CppScheduleOne namespace (IL2CPP variant).
    /// </summary>
    protected static bool IsIl2CppScheduleOneType(Type? type)
    {
        return type?.Namespace?.StartsWith("Il2CppScheduleOne.", StringComparison.Ordinal) == true
            || type?.Namespace == "Il2CppScheduleOne";
    }
    
    /// <summary>
    /// Normalize a type name from Il2CppScheduleOne to ScheduleOne for matching.
    /// </summary>
    protected static string NormalizeScheduleOneTypeName(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return string.Empty;
        
        if (fullName.StartsWith("Il2CppScheduleOne.", StringComparison.Ordinal))
            return "ScheduleOne." + fullName[18..];
        
        return fullName;
    }
}

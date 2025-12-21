using System.Reflection;
using System.Reflection.Emit;
using S1APICoverageAnalyzer.Configuration;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Analyzes the S1API assembly to find which game types are wrapped/covered.
/// </summary>
public sealed class ApiAssemblyAnalyzer : AssemblyAnalyzer
{
    private readonly HashSet<string> _wrappedGameTypes = new();
    private readonly Dictionary<string, HashSet<string>> _typeToAccessedMembers = new();
    private readonly List<ApiTypeInfo> _apiTypes = new();
    
    public ApiAssemblyAnalyzer(Assembly assembly, string assemblyPath) 
        : base(assembly, assemblyPath)
    {
    }
    
    /// <summary>
    /// Analyze the S1API assembly to find all wrapped game types.
    /// </summary>
    public void Analyze()
    {
        foreach (var type in GetAllTypes())
        {
            // Skip non-S1API types
            if (!type.Namespace?.StartsWith("S1API", StringComparison.Ordinal) ?? true)
                continue;
            
            // Include Internal types now - they DO use game types and contribute to coverage
            // (patches wrap game behavior, helpers use game types, etc.)
            
            var apiTypeInfo = new ApiTypeInfo
            {
                FullName = type.FullName ?? type.Name,
                Name = type.Name
            };
            
            // Strategy 1: Primary Wrapper Fields (S1*, Inner*, etc.)
            // Also relaxed to check if field name contains type name
            AnalyzeWrapperFields(type, apiTypeInfo);
            
            // Strategy 2: Component Property (common pattern for component wrappers)
            AnalyzeComponentProperties(type, apiTypeInfo);
            
            // Strategy 3: Same-Name Heuristic (Class 'X' wrapping 'ScheduleOne...X')
            // Checks fields, properties, and method parameters
            AnalyzeSameNameWrapping(type, apiTypeInfo);
            
            // Strategy 4: All Fields and Properties - scan for any game type references
            AnalyzeAllFieldsAndProperties(type, apiTypeInfo);
            
            // Strategy 5: Method signatures - scan parameters and return types
            AnalyzeMethodSignatures(type, apiTypeInfo);
            
            // Strategy 6: Method bodies - scan for type references in IL
            AnalyzeMethodBodies(type, apiTypeInfo);
            
            // Strategy 7: Base types and interfaces
            AnalyzeInheritance(type, apiTypeInfo);
            
            // Strategy 8: Generic type arguments in any context
            AnalyzeGenericArguments(type, apiTypeInfo);
            
            // Strategy 9: Attributes that reference game types
            AnalyzeAttributes(type, apiTypeInfo);
            
            if (apiTypeInfo.WrappedGameTypes.Count > 0)
            {
                _apiTypes.Add(apiTypeInfo);
            }
        }
    }
    
    /// <summary>
    /// Get all game type names that are wrapped by S1API.
    /// </summary>
    public HashSet<string> GetWrappedGameTypes() => _wrappedGameTypes;
    
    /// <summary>
    /// Get mapping of game types to their accessed members.
    /// </summary>
    public Dictionary<string, HashSet<string>> GetAccessedMembers() => _typeToAccessedMembers;
    
    /// <summary>
    /// Get information about all API types that wrap game types.
    /// </summary>
    public List<ApiTypeInfo> GetApiTypes() => _apiTypes;
    
    /// <summary>
    /// Analyze fields that are primary wrappers (S1*, Inner*, etc.).
    /// Only these count as "wrapping" a game type.
    /// </summary>
    private void AnalyzeWrapperFields(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        foreach (var field in GetAllFields(apiType))
        {
            try
            {
                bool isWrapperField = ExclusionConfig.IsWrapperField(field.Name);
                
                // Heuristic: If field name contains the type name, it's likely a wrapper
                // e.g. "_runtimeAvatar" (type Avatar)
                if (!isWrapperField && IsGameType(field.FieldType))
                {
                    string typeName = field.FieldType.Name;
                    if (field.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isWrapperField = true;
                    }
                }
                
                if (!isWrapperField)
                    continue;
                
                RegisterGameTypeReference(field.FieldType, apiType, apiTypeInfo);
            }
            catch
            {
                // Skip fields that cause reflection errors
            }
        }
    }
    
    /// <summary>
    /// Analyze properties named "Component" or similar that return a game type.
    /// </summary>
    private void AnalyzeComponentProperties(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        foreach (var prop in GetAllProperties(apiType))
        {
            try
            {
                if (prop.Name.Equals("Component", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("Wrapped", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("Original", StringComparison.OrdinalIgnoreCase))
                {
                    RegisterGameTypeReference(prop.PropertyType, apiType, apiTypeInfo);
                }
            }
            catch
            {
                // Skip properties that cause reflection errors
            }
        }
    }
    
    /// <summary>
    /// Analyze if the class wraps a game type with the same name.
    /// e.g. S1API.Entities.NPCInventory -> ScheduleOne.NPCs.NPCInventory
    /// </summary>
    private void AnalyzeSameNameWrapping(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        // Look for any member that returns/uses a game type with the same name as this class
        var simpleName = apiType.Name;
        
        // Check properties
        foreach (var prop in GetAllProperties(apiType))
        {
            try
            {
                var type = prop.PropertyType;
                if (IsGameType(type) && type.Name == simpleName)
                {
                     RegisterGameTypeReference(type, apiType, apiTypeInfo);
                }
            }
            catch {}
        }
        
        // Check fields
        foreach (var field in GetAllFields(apiType))
        {
            try
            {
                var type = field.FieldType;
                if (IsGameType(type) && type.Name == simpleName)
                {
                     RegisterGameTypeReference(type, apiType, apiTypeInfo);
                }
            }
            catch {}
        }
        
        // Check methods (including constructors)
        try
        {
            foreach (var method in apiType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                try
                {
                    // Check return type
                    if (IsGameType(method.ReturnType) && method.ReturnType.Name == simpleName)
                    {
                        RegisterGameTypeReference(method.ReturnType, apiType, apiTypeInfo);
                    }
                    
                    // Check parameters
                    foreach (var param in method.GetParameters())
                    {
                        if (IsGameType(param.ParameterType) && param.ParameterType.Name == simpleName)
                        {
                            RegisterGameTypeReference(param.ParameterType, apiType, apiTypeInfo);
                        }
                    }
                }
                catch {}
            }
            
            foreach (var ctor in apiType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                try
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        if (IsGameType(param.ParameterType) && param.ParameterType.Name == simpleName)
                        {
                            RegisterGameTypeReference(param.ParameterType, apiType, apiTypeInfo);
                        }
                    }
                }
                catch {}
            }
        }
        catch {}
    }
    
    private void RegisterGameTypeReference(Type type, Type apiType, ApiTypeInfo apiTypeInfo)
    {
        if (IsGameType(type))
        {
            var normalizedName = NormalizeScheduleOneTypeName(type.FullName);
            if (!string.IsNullOrEmpty(normalizedName))
            {
                _wrappedGameTypes.Add(normalizedName);
                apiTypeInfo.WrappedGameTypes.Add(normalizedName);
                TrackTypeAccess(normalizedName, apiType);
            }
        }
        
        // Also check generic type arguments
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                if (IsGameType(arg))
                {
                    var normalizedName = NormalizeScheduleOneTypeName(arg.FullName);
                    if (!string.IsNullOrEmpty(normalizedName))
                    {
                        _wrappedGameTypes.Add(normalizedName);
                        apiTypeInfo.WrappedGameTypes.Add(normalizedName);
                        TrackTypeAccess(normalizedName, apiType);
                    }
                }
            }
        }
    }
    
    private bool IsGameType(Type type)
    {
        return IsScheduleOneType(type) || IsIl2CppScheduleOneType(type);
    }
    
    private void TrackTypeAccess(string gameTypeName, Type apiType)
    {
        if (!_typeToAccessedMembers.ContainsKey(gameTypeName))
        {
            _typeToAccessedMembers[gameTypeName] = new HashSet<string>();
        }
    }
    
    /// <summary>
    /// Analyze all fields and properties for game type references, not just wrapper fields.
    /// </summary>
    private void AnalyzeAllFieldsAndProperties(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        // Check all fields
        foreach (var field in GetAllFields(apiType))
        {
            try
            {
                RegisterGameTypeReference(field.FieldType, apiType, apiTypeInfo);
            }
            catch { }
        }
        
        // Check all properties
        foreach (var prop in GetAllProperties(apiType))
        {
            try
            {
                RegisterGameTypeReference(prop.PropertyType, apiType, apiTypeInfo);
            }
            catch { }
        }
    }
    
    /// <summary>
    /// Analyze method signatures for game type references in parameters and return types.
    /// </summary>
    private void AnalyzeMethodSignatures(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            
            // Methods
            foreach (var method in apiType.GetMethods(flags))
            {
                try
                {
                    // Return type
                    RegisterGameTypeReference(method.ReturnType, apiType, apiTypeInfo);
                    
                    // Parameters
                    foreach (var param in method.GetParameters())
                    {
                        RegisterGameTypeReference(param.ParameterType, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
            
            // Constructors
            foreach (var ctor in apiType.GetConstructors(flags))
            {
                try
                {
                    foreach (var param in ctor.GetParameters())
                    {
                        RegisterGameTypeReference(param.ParameterType, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Analyze method bodies for game type references in IL code.
    /// This catches static method calls, type instantiation, field accesses, etc.
    /// </summary>
    private void AnalyzeMethodBodies(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            
            foreach (var method in apiType.GetMethods(flags))
            {
                try
                {
                    var body = method.GetMethodBody();
                    if (body == null) continue;
                    
                    // Check local variables
                    foreach (var local in body.LocalVariables)
                    {
                        RegisterGameTypeReference(local.LocalType, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
            
            // Also check constructors
            foreach (var ctor in apiType.GetConstructors(flags))
            {
                try
                {
                    var body = ctor.GetMethodBody();
                    if (body == null) continue;
                    
                    foreach (var local in body.LocalVariables)
                    {
                        RegisterGameTypeReference(local.LocalType, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Analyze base types and implemented interfaces for game type references.
    /// </summary>
    private void AnalyzeInheritance(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            // Base type
            if (apiType.BaseType != null)
            {
                RegisterGameTypeReference(apiType.BaseType, apiType, apiTypeInfo);
            }
            
            // Interfaces
            foreach (var iface in apiType.GetInterfaces())
            {
                RegisterGameTypeReference(iface, apiType, apiTypeInfo);
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Analyze generic type arguments used anywhere in the type.
    /// </summary>
    private void AnalyzeGenericArguments(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            // Type-level generic arguments
            if (apiType.IsGenericType)
            {
                foreach (var arg in apiType.GetGenericArguments())
                {
                    RegisterGameTypeReference(arg, apiType, apiTypeInfo);
                }
            }
            
            // Check fields for generic arguments
            foreach (var field in GetAllFields(apiType))
            {
                try
                {
                    ExtractGenericArguments(field.FieldType, apiType, apiTypeInfo);
                }
                catch { }
            }
            
            // Check properties for generic arguments
            foreach (var prop in GetAllProperties(apiType))
            {
                try
                {
                    ExtractGenericArguments(prop.PropertyType, apiType, apiTypeInfo);
                }
                catch { }
            }
        }
        catch { }
    }
    
    private void ExtractGenericArguments(Type type, Type apiType, ApiTypeInfo apiTypeInfo)
    {
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                RegisterGameTypeReference(arg, apiType, apiTypeInfo);
                ExtractGenericArguments(arg, apiType, apiTypeInfo);
            }
        }
        
        // Also check array element types
        if (type.IsArray && type.GetElementType() != null)
        {
            RegisterGameTypeReference(type.GetElementType()!, apiType, apiTypeInfo);
            ExtractGenericArguments(type.GetElementType()!, apiType, apiTypeInfo);
        }
    }
    
    /// <summary>
    /// Analyze custom attributes for game type references.
    /// </summary>
    private void AnalyzeAttributes(Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            // Type attributes
            foreach (var attr in apiType.GetCustomAttributesData())
            {
                try
                {
                    RegisterGameTypeReference(attr.AttributeType, apiType, apiTypeInfo);
                    
                    // Check constructor arguments
                    foreach (var arg in attr.ConstructorArguments)
                    {
                        if (arg.Value is Type typeArg)
                        {
                            RegisterGameTypeReference(typeArg, apiType, apiTypeInfo);
                        }
                    }
                }
                catch { }
            }
            
            // Method/property/field attributes (HarmonyPatch is a big one)
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            
            foreach (var method in apiType.GetMethods(flags))
            {
                try
                {
                    foreach (var attr in method.GetCustomAttributesData())
                    {
                        AnalyzeAttributeData(attr, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
            
            foreach (var field in apiType.GetFields(flags))
            {
                try
                {
                    foreach (var attr in field.GetCustomAttributesData())
                    {
                        AnalyzeAttributeData(attr, apiType, apiTypeInfo);
                    }
                }
                catch { }
            }
        }
        catch { }
    }
    
    private void AnalyzeAttributeData(CustomAttributeData attr, Type apiType, ApiTypeInfo apiTypeInfo)
    {
        try
        {
            // Check if this is a HarmonyPatch or similar that references a game type
            foreach (var arg in attr.ConstructorArguments)
            {
                if (arg.Value is Type typeArg)
                {
                    RegisterGameTypeReference(typeArg, apiType, apiTypeInfo);
                }
            }
            
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.TypedValue.Value is Type typeArg)
                {
                    RegisterGameTypeReference(typeArg, apiType, apiTypeInfo);
                }
            }
        }
        catch { }
    }
}

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

            if (type.DeclaringType != null && IsGameType(type.DeclaringType))
            {
                RegisterGameTypeReference(type.DeclaringType, apiType, apiTypeInfo);
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
    /// Also checks generic method constraints.
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
                    
                    // Check generic method constraints
                    if (method.IsGenericMethodDefinition)
                    {
                        foreach (var genericParam in method.GetGenericArguments())
                        {
                            // Check constraints (base type, interfaces)
                            foreach (var constraint in genericParam.GetGenericParameterConstraints())
                            {
                                RegisterGameTypeReference(constraint, apiType, apiTypeInfo);
                            }
                        }
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
    /// This catches local variables and attempts to extract type references from IL.
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
                    AnalyzeMethodBody(method, apiType, apiTypeInfo);
                }
                catch { }
            }
            
            // Also check constructors
            foreach (var ctor in apiType.GetConstructors(flags))
            {
                try
                {
                    AnalyzeMethodBody(ctor, apiType, apiTypeInfo);
                }
                catch { }
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Analyze a single method body for game type references.
    /// </summary>
    private void AnalyzeMethodBody(MethodBase method, Type apiType, ApiTypeInfo apiTypeInfo)
    {
        var body = method.GetMethodBody();
        if (body == null) return;
        
        // Check local variables
        foreach (var local in body.LocalVariables)
        {
            RegisterGameTypeReference(local.LocalType, apiType, apiTypeInfo);
        }
        
        // Try to extract type references from IL tokens
        try
        {
            var ilBytes = body.GetILAsByteArray();
            if (ilBytes == null || ilBytes.Length == 0) return;
            
            // Simple approach: scan for type tokens in IL
            // Look for ldtoken, call, callvirt, newobj instructions that reference types
            ExtractTypesFromIL(ilBytes, method.Module, apiType, apiTypeInfo);
        }
        catch
        {
            // If IL parsing fails, that's okay - we've already checked local variables
        }
    }
    
    /// <summary>
    /// Extract type references from IL bytecode by scanning for type tokens.
    /// </summary>
    private void ExtractTypesFromIL(byte[] ilBytes, Module module, Type apiType, ApiTypeInfo apiTypeInfo)
    {
        // Scan for method call tokens (0x28 call, 0x6F callvirt, 0x73 newobj)
        // These are followed by 4-byte metadata tokens
        for (int i = 0; i < ilBytes.Length - 4; i++)
        {
            byte opcode = ilBytes[i];
            
            // Check for call (0x28), callvirt (0x6F), newobj (0x73), ldtoken (0xD0)
            if (opcode == 0x28 || opcode == 0x6F || opcode == 0x73 || opcode == 0xD0)
            {
                // Check for two-byte opcode prefix (0xFE)
                if (i > 0 && ilBytes[i - 1] == 0xFE)
                {
                    // Two-byte opcode, skip
                    continue;
                }
                
                // Read the token (4 bytes, little-endian)
                int token = BitConverter.ToInt32(ilBytes, i + 1);
                
                try
                {
                    // Try to resolve as method (most common for call/callvirt/newobj)
                    if (opcode == 0x28 || opcode == 0x6F || opcode == 0x73)
                    {
                        var member = module.ResolveMethod(token);
                        if (member is MethodInfo method)
                        {
                            if (method.DeclaringType != null)
                            {
                                RegisterGameTypeReference(method.DeclaringType, apiType, apiTypeInfo);
                            }

                            // Check if this is a generic method with game type arguments
                            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                            {
                                foreach (var arg in method.GetGenericArguments())
                                {
                                    RegisterGameTypeReference(arg, apiType, apiTypeInfo);
                                }
                            }
                            
                            // Check return type and parameters
                            RegisterGameTypeReference(method.ReturnType, apiType, apiTypeInfo);
                            foreach (var param in method.GetParameters())
                            {
                                RegisterGameTypeReference(param.ParameterType, apiType, apiTypeInfo);
                            }
                        }
                    }
                    // Try to resolve as type (for ldtoken)
                    else if (opcode == 0xD0)
                    {
                        var type = module.ResolveType(token);
                        RegisterGameTypeReference(type, apiType, apiTypeInfo);
                    }
                }
                catch
                {
                    // Token resolution failed, skip
                }
            }
        }
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

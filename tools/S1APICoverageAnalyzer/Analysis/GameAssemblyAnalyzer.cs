using System.Reflection;
using S1APICoverageAnalyzer.Configuration;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Analyzes the game assembly (Assembly-CSharp.dll) to extract all ScheduleOne types and their members.
/// </summary>
public sealed class GameAssemblyAnalyzer : AssemblyAnalyzer
{
    public GameAssemblyAnalyzer(Assembly assembly, string assemblyPath) 
        : base(assembly, assemblyPath)
    {
    }
    
    /// <summary>
    /// Extract all eligible game types from the ScheduleOne namespace.
    /// </summary>
    public List<GameType> ExtractGameTypes()
    {
        var gameTypes = new List<GameType>();
        
        foreach (var type in GetPublicTypes())
        {
            // Only include types in the ScheduleOne namespace
            if (!IsScheduleOneType(type))
                continue;
            
            // Skip excluded namespaces
            if (ExclusionConfig.IsNamespaceExcluded(type.Namespace))
                continue;
            
            // Skip infrastructure types
            if (ExclusionConfig.IsInfrastructureNamespace(type.Namespace))
                continue;
            
            // Skip excluded type patterns
            if (ExclusionConfig.IsTypeExcluded(type.Name))
                continue;
            
            // Skip compiler-generated types
            if (type.Name.StartsWith("<") || type.Name.Contains("__"))
                continue;
            
            var gameType = CreateGameType(type);
            if (gameType != null)
                gameTypes.Add(gameType);
        }
        
        return gameTypes;
    }
    
    /// <summary>
    /// Get count of excluded types for reporting.
    /// </summary>
    public int CountExcludedTypes()
    {
        int count = 0;
        
        foreach (var type in GetPublicTypes())
        {
            if (!IsScheduleOneType(type))
                continue;
            
            if (ExclusionConfig.IsNamespaceExcluded(type.Namespace) ||
                ExclusionConfig.IsInfrastructureNamespace(type.Namespace) ||
                ExclusionConfig.IsTypeExcluded(type.Name))
            {
                count++;
            }
        }
        
        return count;
    }
    
    private GameType? CreateGameType(Type type)
    {
        try
        {
            var kind = GetTypeKind(type);
            
            var gameType = new GameType
            {
                FullName = type.FullName ?? type.Name,
                Namespace = type.Namespace ?? "ScheduleOne",
                Name = type.Name,
                Kind = kind
            };
            
            // Extract members (skip for enums - they're just values)
            if (kind != GameTypeKind.Enum && kind != GameTypeKind.Delegate)
            {
                ExtractMembers(type, gameType);
            }
            
            return gameType;
        }
        catch
        {
            // Skip types that cause reflection errors
            return null;
        }
    }
    
    private static GameTypeKind GetTypeKind(Type type)
    {
        if (type.IsEnum) return GameTypeKind.Enum;
        if (type.IsInterface) return GameTypeKind.Interface;
        if (type.IsValueType) return GameTypeKind.Struct;
        if (typeof(Delegate).IsAssignableFrom(type)) return GameTypeKind.Delegate;
        return GameTypeKind.Class;
    }
    
    private void ExtractMembers(Type type, GameType gameType)
    {
        // Extract public fields
        foreach (var field in GetPublicFields(type))
        {
            // Skip generated fields
            if (ExclusionConfig.IsGeneratedField(field.Name))
                continue;
            
            // Skip backing fields
            if (field.Name.Contains("k__BackingField"))
                continue;
            
            gameType.Members.Add(new GameMember
            {
                Name = field.Name,
                Kind = GameMemberKind.Field,
                ReturnType = GetSafeTypeName(field.FieldType),
                IsStatic = field.IsStatic
            });
        }
        
        // Extract public properties
        foreach (var prop in GetPublicProperties(type))
        {
            gameType.Members.Add(new GameMember
            {
                Name = prop.Name,
                Kind = GameMemberKind.Property,
                ReturnType = GetSafeTypeName(prop.PropertyType),
                IsStatic = prop.GetMethod?.IsStatic ?? prop.SetMethod?.IsStatic ?? false
            });
        }
        
        // Extract public methods
        foreach (var method in GetPublicMethods(type))
        {
            // Skip generated networking methods
            if (ExclusionConfig.IsGeneratedMethod(method.Name))
                continue;
            
            // Skip constructors shown as methods
            if (method.IsConstructor)
                continue;
            
            gameType.Members.Add(new GameMember
            {
                Name = method.Name,
                Kind = GameMemberKind.Method,
                ReturnType = GetSafeTypeName(method.ReturnType),
                IsStatic = method.IsStatic
            });
        }
        
        // Extract public events
        foreach (var evt in GetPublicEvents(type))
        {
            gameType.Members.Add(new GameMember
            {
                Name = evt.Name,
                Kind = GameMemberKind.Event,
                ReturnType = GetSafeTypeName(evt.EventHandlerType),
                IsStatic = evt.AddMethod?.IsStatic ?? false
            });
        }
    }
}

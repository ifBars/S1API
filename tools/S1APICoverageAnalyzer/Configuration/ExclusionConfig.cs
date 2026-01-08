namespace S1APICoverageAnalyzer.Configuration;

/// <summary>
/// Configuration for excluding certain namespaces, types, and members from coverage analysis.
/// </summary>
public static class ExclusionConfig
{
    /// <summary>
    /// Namespace patterns to exclude from the game assembly analysis.
    /// Types in these namespaces won't count toward coverage.
    /// These are infrastructure, internal systems, or types not relevant for modding.
    /// </summary>
    public static readonly string[] ExcludedNamespaces =
    [
        // Persistence/serialization infrastructure
        "ScheduleOne.Persistence", // Exclude entire persistence layer
        
        // Internal game systems not meant for modding
        "ScheduleOne.VoiceChat",
        "ScheduleOne.Tutorials",
        "ScheduleOne.Tools", // Internal tools
        "ScheduleOne.DevUtilities", // Internal dev utilities
        
        // FishNet networking internals
        "ScheduleOne.Networking",
        
        // Audio system (internal implementation, likely not wrapped)
        "ScheduleOne.Audio",
        "ScheduleOne.VoiceOver",
        "ScheduleOne.Noise",
        
        // Visuals / Rendering / FX (internal implementation)
        "ScheduleOne.FX",
        "ScheduleOne.Effects",
        "ScheduleOne.Lighting",
        "ScheduleOne.Materials",
        "ScheduleOne.Packaging", // Visuals for packaging
        "ScheduleOne.PostProcessing",
        "ScheduleOne.Shaders",
        
        // Physics / Math / Core Utils
        "ScheduleOne.GamePhysics",
        "ScheduleOne.Physics",
        "ScheduleOne.Math",
        "ScheduleOne.Tiles",
        "ScheduleOne.Variables", // Internal variables system
        "ScheduleOne.Vision", // Internal NPC vision logic
        "ScheduleOne.Polling", // Internal polling/feedback
        "ScheduleOne.Dragging",
        "ScheduleOne.Decoration",
        
        // Avatar internals (Animation, Rendering, etc.) - API wraps high level Avatar only
        "ScheduleOne.AvatarFramework.Animation",
        "ScheduleOne.AvatarFramework.Emotions",
        "ScheduleOne.AvatarFramework.Impostors",
        "ScheduleOne.AvatarFramework.Rendering",
        "ScheduleOne.AvatarFramework.Customization", // Internal replicators and UI
        "ScheduleOne.AvatarFramework.Equipping", // Visual representation of equipped items
        
        // Camera systems
        "ScheduleOne.Camera",
        "ScheduleOne.PlayerScripts.Camera",
        
        // UI - Entire UI namespace is internal implementation
        "ScheduleOne.UI",
        
        // Calling internals (CallManager is wrapped, but PayPhone etc are not)
        "ScheduleOne.Calling",
        
        // NPC Character Classes - empty network stubs that just inherit from NPC
        // The base NPC type is wrapped, and S1API has its own named NPC wrappers
        "ScheduleOne.NPCs.CharacterClasses",
        
        // Cinematics
        "ScheduleOne.Cutscenes",
        "ScheduleOne.Intro",
        
        // Player Tasks (Mini-games internals)
        "ScheduleOne.PlayerTasks",
        "Casino.UI",
        "ScheduleOne.Console",
    ];
    
    /// <summary>
    /// Namespace patterns that contain infrastructure/utility types that shouldn't count.
    /// </summary>
    public static readonly string[] InfrastructureNamespaces =
    [
        // Kept for backward compatibility if analyzer uses it separately
    ];
    
    /// <summary>
    /// Specific type names to exclude (can be partial matches).
    /// </summary>
    public static readonly string[] ExcludedTypePatterns =
    [
        "Singleton`1",
        "NetworkSingleton`1",
        "PersistentSingleton`1",
        "PlayerSingleton`1",
        "<PrivateImplementationDetails>",
        "<>c", // Compiler generated closures
        "<>c__DisplayClass",
        "<Module>",
        "CanvasScaler",
        "GraphicRaycaster",
        "LayoutGroup",
        
        // Specific types that shouldn't count toward coverage
        "AchievementManager", // Internal achievement system
        "IGUIDRegisterable", // Internal interface
        "Stan",
        "Meg",
        "Jerry",
        "Doris",
        "Donna",
        "Chloe",
        "Billy",
        "CashSlot",
        "IMessageEntity",
        "ResponseCallback",
        "WaterCollider",
        "BranchNodeData",
        "BranchOptionData",
        "EClothingColor",
        "SlotReel",
        "AvatarLayer",
        "Eyebrow",
        "FaceLayer",
        "Hair",
        "Registry",
        "ForkliftCamera",
        "IStorageEntity",
        "CosmeticPowerLine",
        "AvatarLODBoundsUpdater",
    ];
    
    /// <summary>
    /// Method name prefixes that indicate generated FishNet networking code.
    /// </summary>
    public static readonly string[] GeneratedMethodPrefixes =
    [
        "RpcWriter___",
        "RpcReader___",
        "RpcLogic___",
        "NetworkInitialize___",
        "NetworkInitialize__",
    ];
    
    /// <summary>
    /// Field name patterns that indicate generated code.
    /// </summary>
    public static readonly string[] GeneratedFieldPatterns =
    [
        "NetworkInitialize___",
        "__Excuted", // typo in FishNet generated code
    ];
    
    /// <summary>
    /// Field name prefixes that indicate a primary wrapper field (the main wrapped type).
    /// Only fields matching these patterns will count a game type as "wrapped".
    /// </summary>
    public static readonly string[] WrapperFieldPrefixes =
    [
        "S1",           // e.g., S1NPC, S1LandVehicle, S1Quest
        "_s1",          // private variant
        "Inner",        // e.g., InnerProperty, InnerBusiness
        "_inner",       // private variant
        "<S1",          // backing field like <S1Quest>k__BackingField
        "<Inner",       // backing field like <InnerProperty>k__BackingField
    ];
    
    /// <summary>
    /// Check if a namespace should be excluded.
    /// </summary>
    public static bool IsNamespaceExcluded(string? ns)
    {
        if (string.IsNullOrEmpty(ns)) return true;
        
        foreach (var pattern in ExcludedNamespaces)
        {
            if (ns.StartsWith(pattern, StringComparison.Ordinal))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a namespace is infrastructure (excluded from coverage but tracked).
    /// </summary>
    public static bool IsInfrastructureNamespace(string? ns)
    {
        if (string.IsNullOrEmpty(ns)) return false;
        
        foreach (var pattern in InfrastructureNamespaces)
        {
            if (ns.StartsWith(pattern, StringComparison.Ordinal))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a type should be excluded based on its name.
    /// </summary>
    public static bool IsTypeExcluded(string typeName)
    {
        foreach (var pattern in ExcludedTypePatterns)
        {
            if (typeName.Contains(pattern))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a method is generated FishNet networking code.
    /// </summary>
    public static bool IsGeneratedMethod(string methodName)
    {
        foreach (var prefix in GeneratedMethodPrefixes)
        {
            if (methodName.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a field is generated code.
    /// </summary>
    public static bool IsGeneratedField(string fieldName)
    {
        foreach (var pattern in GeneratedFieldPatterns)
        {
            if (fieldName.Contains(pattern))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a field name indicates it's the primary wrapper field for a game type.
    /// </summary>
    public static bool IsWrapperField(string fieldName)
    {
        foreach (var prefix in WrapperFieldPrefixes)
        {
            if (fieldName.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }
        
        return false;
    }
}

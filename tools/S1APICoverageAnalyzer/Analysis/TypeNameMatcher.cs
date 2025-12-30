using System.Text.RegularExpressions;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Provides fuzzy matching logic for game types to S1API types.
/// Handles common naming conventions like enum prefixes, plural/singular, etc.
/// </summary>
public static class TypeNameMatcher
{
    private static readonly Regex EnumPrefixPattern = new(@"^E([A-Z])", RegexOptions.Compiled);
    private static readonly Regex InterfacePrefixPattern = new(@"^I([A-Z])", RegexOptions.Compiled);
    
    /// <summary>
    /// Calculate similarity score between a game type name and an S1API type name.
    /// Returns a score from 0.0 (no match) to 1.0 (exact match).
    /// </summary>
    public static double CalculateSimilarity(string gameTypeFullName, string apiTypeFullName, string apiTypeSimpleName)
    {
        // Exact match is perfect
        if (gameTypeFullName.Equals(apiTypeFullName, StringComparison.Ordinal))
            return 1.0;
        
        // Extract simple names for comparison
        var gameSimpleName = ExtractSimpleName(gameTypeFullName);
        
        // Exact simple name match is very strong
        if (gameSimpleName.Equals(apiTypeSimpleName, StringComparison.Ordinal))
            return 0.95;
        
        // Normalize both names (remove common prefixes)
        var normalizedGameName = NormalizeTypeName(gameSimpleName);
        var normalizedApiName = NormalizeTypeName(apiTypeSimpleName);
        
        // Exact match after normalization
        if (normalizedGameName.Equals(normalizedApiName, StringComparison.Ordinal))
            return 0.9;
        
        // Case-insensitive match after normalization
        if (normalizedGameName.Equals(normalizedApiName, StringComparison.OrdinalIgnoreCase))
            return 0.85;
        
        // Plural/singular variations
        if (ArePluraLSingularVariants(normalizedGameName, normalizedApiName))
            return 0.8;
        
        // Substring matching (one contains the other)
        if (normalizedGameName.Contains(normalizedApiName, StringComparison.OrdinalIgnoreCase) ||
            normalizedApiName.Contains(normalizedGameName, StringComparison.OrdinalIgnoreCase))
        {
            return 0.7;
        }
        
        // Levenshtein distance for close matches
        var distance = LevenshteinDistance(normalizedGameName.ToLowerInvariant(), normalizedApiName.ToLowerInvariant());
        var maxLen = Math.Max(normalizedGameName.Length, normalizedApiName.Length);
        if (maxLen == 0) return 0.0;
        
        var similarity = 1.0 - ((double)distance / maxLen);
        
        // Only consider it a match if similarity is reasonably high
        if (similarity >= 0.7)
            return similarity * 0.6; // Scale down fuzzy matches
        
        return 0.0;
    }
    
    /// <summary>
    /// Extract the simple name from a full type name (last part after dot or plus).
    /// </summary>
    private static string ExtractSimpleName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;
        
        // Handle nested types (+ separator)
        var plusIndex = fullName.LastIndexOf('+');
        if (plusIndex >= 0)
            return fullName[(plusIndex + 1)..];
        
        // Handle namespace separator
        var dotIndex = fullName.LastIndexOf('.');
        if (dotIndex >= 0)
            return fullName[(dotIndex + 1)..];
        
        return fullName;
    }
    
    /// <summary>
    /// Normalize a type name by removing common prefixes and suffixes.
    /// E.g., "EVehicleColor" -> "VehicleColor", "IPlayerService" -> "PlayerService"
    /// </summary>
    public static string NormalizeTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return string.Empty;
        
        var normalized = typeName;
        
        // Remove enum prefix (E + uppercase letter)
        normalized = EnumPrefixPattern.Replace(normalized, "$1");
        
        // Remove interface prefix (I + uppercase letter)
        normalized = InterfacePrefixPattern.Replace(normalized, "$1");
        
        // Remove common suffixes
        normalized = RemoveSuffix(normalized, "Component");
        normalized = RemoveSuffix(normalized, "Behaviour");
        normalized = RemoveSuffix(normalized, "Behavior");
        normalized = RemoveSuffix(normalized, "Manager");
        normalized = RemoveSuffix(normalized, "Controller");
        normalized = RemoveSuffix(normalized, "Service");
        normalized = RemoveSuffix(normalized, "Handler");
        normalized = RemoveSuffix(normalized, "Helper");
        normalized = RemoveSuffix(normalized, "Utility");
        normalized = RemoveSuffix(normalized, "Util");
        
        return normalized;
    }
    
    private static string RemoveSuffix(string text, string suffix)
    {
        if (text.EndsWith(suffix, StringComparison.Ordinal) && text.Length > suffix.Length)
            return text[..^suffix.Length];
        return text;
    }
    
    /// <summary>
    /// Check if two names are plural/singular variants of each other.
    /// </summary>
    private static bool ArePluraLSingularVariants(string name1, string name2)
    {
        // Simple heuristic: one ends with 's' and removing it makes them equal
        if (name1.Length == name2.Length + 1 && name1.EndsWith('s'))
        {
            return name1[..^1].Equals(name2, StringComparison.OrdinalIgnoreCase);
        }
        
        if (name2.Length == name1.Length + 1 && name2.EndsWith('s'))
        {
            return name2[..^1].Equals(name1, StringComparison.OrdinalIgnoreCase);
        }
        
        // Check for "ies" -> "y" pattern (e.g., "Categories" -> "Category")
        if (name1.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name1.Length > 3)
        {
            var singular = name1[..^3] + "y";
            if (singular.Equals(name2, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        if (name2.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name2.Length > 3)
        {
            var singular = name2[..^3] + "y";
            if (singular.Equals(name1, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        
        if (string.IsNullOrEmpty(t))
            return s.Length;
        
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        
        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        
        for (int j = 0; j <= m; j++)
            d[0, j] = j;
        
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        
        return d[n, m];
    }
    
    /// <summary>
    /// Get all possible name variations for a type that should be tried for matching.
    /// </summary>
    public static IEnumerable<string> GetNameVariations(string typeName)
    {
        yield return typeName;
        
        var normalized = NormalizeTypeName(typeName);
        if (normalized != typeName)
            yield return normalized;
        
        // Try adding/removing common enum prefix
        if (typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            if (typeName[0] == 'E')
            {
                // Remove E prefix
                yield return typeName[1..];
            }
            else
            {
                // Add E prefix
                yield return "E" + typeName;
            }
        }
        
        // Try plural/singular
        if (typeName.EndsWith('s') && typeName.Length > 2)
        {
            yield return typeName[..^1];
        }
        else
        {
            yield return typeName + "s";
        }
        
        if (typeName.EndsWith("ies", StringComparison.Ordinal) && typeName.Length > 3)
        {
            yield return typeName[..^3] + "y";
        }
        else if (typeName.EndsWith('y') && typeName.Length > 1)
        {
            yield return typeName[..^1] + "ies";
        }
    }
}


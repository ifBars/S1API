using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Output;

/// <summary>
/// Generates shields.io badge URLs for coverage visualization.
/// </summary>
public static class BadgeGenerator
{
    /// <summary>
    /// Generate a shields.io badge URL for class-level coverage.
    /// </summary>
    public static string GenerateClassCoverageBadgeUrl(CoverageResult result)
    {
        var percentage = result.ClassCoveragePercentage;
        var color = GetColorForPercentage(percentage);
        var label = "API Coverage";
        var value = $"{percentage:F1}%";
        
        // URL encode the values
        var encodedLabel = Uri.EscapeDataString(label);
        var encodedValue = Uri.EscapeDataString(value);
        
        return $"https://img.shields.io/badge/{encodedLabel}-{encodedValue}-{color}";
    }
    
    /// <summary>
    /// Generate a shields.io badge URL for member-level coverage.
    /// </summary>
    public static string GenerateMemberCoverageBadgeUrl(CoverageResult result)
    {
        var percentage = result.MemberCoveragePercentage;
        var color = GetColorForPercentage(percentage);
        var label = "Member Coverage";
        var value = $"{percentage:F1}%";
        
        var encodedLabel = Uri.EscapeDataString(label);
        var encodedValue = Uri.EscapeDataString(value);
        
        return $"https://img.shields.io/badge/{encodedLabel}-{encodedValue}-{color}";
    }
    
    /// <summary>
    /// Generate markdown for the class coverage badge.
    /// </summary>
    public static string GenerateClassCoverageBadgeMarkdown(CoverageResult result)
    {
        var url = GenerateClassCoverageBadgeUrl(result);
        return $"[![API Coverage]({url})](docs/coverage-report.json)";
    }
    
    /// <summary>
    /// Generate a combined badge line with both class and member coverage.
    /// </summary>
    public static string GenerateCombinedBadgeMarkdown(CoverageResult result)
    {
        var classUrl = GenerateClassCoverageBadgeUrl(result);
        var memberUrl = GenerateMemberCoverageBadgeUrl(result);
        
        return $"[![API Coverage]({classUrl})]() [![Member Coverage]({memberUrl})]()";
    }
    
    /// <summary>
    /// Get the badge color based on coverage percentage.
    /// </summary>
    private static string GetColorForPercentage(double percentage)
    {
        return percentage switch
        {
            >= 80 => "brightgreen",
            >= 60 => "yellowgreen", 
            >= 40 => "yellow",
            >= 20 => "orange",
            _ => "red"
        };
    }
}

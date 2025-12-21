using System.Text.Json;
using System.Text.Json.Serialization;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Output;

/// <summary>
/// Generates detailed coverage reports in various formats.
/// </summary>
public static class ReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    /// <summary>
    /// Generate a JSON report of coverage results.
    /// </summary>
    public static string GenerateJsonReport(CoverageResult result)
    {
        var report = new CoverageReport
        {
            Timestamp = result.Timestamp,
            ClassCoverage = new CoverageMetrics
            {
                Total = result.TotalGameClasses,
                Covered = result.CoveredGameClasses,
                Percentage = Math.Round(result.ClassCoveragePercentage, 2)
            },
            MemberCoverage = new CoverageMetrics
            {
                Total = result.TotalGameMembers,
                Covered = result.CoveredGameMembers,
                Percentage = Math.Round(result.MemberCoveragePercentage, 2)
            },
            ExcludedTypeCount = result.ExcludedTypeCount,
            ExcludedNamespaces = result.ExcludedNamespaces,
            CoveredTypes = result.CoveredTypes
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .Select(t => new TypeCoverageInfo
                {
                    FullName = t.FullName,
                    CoveredBy = t.CoveredByApiType,
                    MembersCovered = t.CoveredMemberCount,
                    MembersTotal = t.TotalMemberCount
                })
                .ToList(),
            UncoveredTypes = result.UncoveredTypes
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .Select(t => t.FullName)
                .ToList(),
            ApiTypes = result.ApiTypes
                .OrderBy(a => a.FullName)
                .Select(a => new ApiTypeSummary
                {
                    FullName = a.FullName,
                    WrappedTypes = a.WrappedGameTypes.OrderBy(x => x).ToList()
                })
                .ToList()
        };
        
        return JsonSerializer.Serialize(report, JsonOptions);
    }
    
    /// <summary>
    /// Generate a console summary of coverage.
    /// </summary>
    public static string GenerateConsoleSummary(CoverageResult result)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              S1API Coverage Analysis Report                  ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Timestamp: {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC                       ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine("║  CLASS COVERAGE                                              ║");
        sb.AppendLine($"║    Total Game Classes:    {result.TotalGameClasses,6}                          ║");
        sb.AppendLine($"║    Covered Classes:       {result.CoveredGameClasses,6}                          ║");
        sb.AppendLine($"║    Coverage Percentage:   {result.ClassCoveragePercentage,6:F2}%                        ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine("║  MEMBER COVERAGE                                             ║");
        sb.AppendLine($"║    Total Game Members:    {result.TotalGameMembers,6}                          ║");
        sb.AppendLine($"║    Covered Members:       {result.CoveredGameMembers,6}                          ║");
        sb.AppendLine($"║    Coverage Percentage:   {result.MemberCoveragePercentage,6:F2}%                        ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║  Excluded Types:          {result.ExcludedTypeCount,6}                          ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Write a JSON report to a file.
    /// </summary>
    public static async Task WriteJsonReportAsync(CoverageResult result, string outputPath)
    {
        var json = GenerateJsonReport(result);
        await File.WriteAllTextAsync(outputPath, json);
    }
    
    /// <summary>
    /// Write a badge markdown to a file.
    /// </summary>
    public static async Task WriteBadgeMarkdownAsync(CoverageResult result, string outputPath)
    {
        var markdown = BadgeGenerator.GenerateClassCoverageBadgeMarkdown(result);
        await File.WriteAllTextAsync(outputPath, markdown);
    }
    
    /// <summary>
    /// Generate a plain text report for easy verification.
    /// </summary>
    public static string GenerateTextReport(CoverageResult result)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("================================================================================");
        sb.AppendLine("                     S1API COVERAGE ANALYSIS REPORT");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"Generated: {result.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine($"CLASS COVERAGE: {result.CoveredGameClasses} / {result.TotalGameClasses} ({result.ClassCoveragePercentage:F2}%)");
        sb.AppendLine($"EXCLUDED TYPES: {result.ExcludedTypeCount}");
        sb.AppendLine();
        
        // Covered types section
        sb.AppendLine("================================================================================");
        sb.AppendLine($"COVERED TYPES ({result.CoveredTypes.Count})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        
        var coveredByNamespace = result.CoveredTypes
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .GroupBy(t => t.Namespace);
        
        foreach (var nsGroup in coveredByNamespace)
        {
            sb.AppendLine($"--- {nsGroup.Key} ---");
            foreach (var type in nsGroup)
            {
                if (!string.IsNullOrEmpty(type.CoveredByApiType))
                {
                    sb.AppendLine($"  [✓] {type.Name}");
                    sb.AppendLine($"      -> {type.CoveredByApiType}");
                }
                else
                {
                    sb.AppendLine($"  [✓] {type.Name}");
                }
            }
            sb.AppendLine();
        }
        
        // Uncovered types section
        sb.AppendLine("================================================================================");
        sb.AppendLine($"UNCOVERED TYPES ({result.UncoveredTypes.Count})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        
        var uncoveredByNamespace = result.UncoveredTypes
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .GroupBy(t => t.Namespace);
        
        foreach (var nsGroup in uncoveredByNamespace)
        {
            sb.AppendLine($"--- {nsGroup.Key} ---");
            foreach (var type in nsGroup)
            {
                sb.AppendLine($"  [ ] {type.Name}");
            }
            sb.AppendLine();
        }
        
        // API types summary
        sb.AppendLine("================================================================================");
        sb.AppendLine($"S1API TYPES THAT WRAP GAME TYPES ({result.ApiTypes.Count})");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        
        foreach (var apiType in result.ApiTypes.OrderBy(a => a.FullName))
        {
            if (apiType.WrappedGameTypes.Count > 0)
            {
                sb.AppendLine($"{apiType.FullName}:");
                foreach (var wrapped in apiType.WrappedGameTypes.OrderBy(x => x))
                {
                    sb.AppendLine($"    - {wrapped}");
                }
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Write a plain text report to a file.
    /// </summary>
    public static async Task WriteTextReportAsync(CoverageResult result, string outputPath)
    {
        var text = GenerateTextReport(result);
        await File.WriteAllTextAsync(outputPath, text);
    }
}

// Report DTOs for JSON serialization
internal sealed class CoverageReport
{
    public DateTime Timestamp { get; init; }
    public required CoverageMetrics ClassCoverage { get; init; }
    public required CoverageMetrics MemberCoverage { get; init; }
    public int ExcludedTypeCount { get; init; }
    public List<string> ExcludedNamespaces { get; init; } = new();
    public List<TypeCoverageInfo> CoveredTypes { get; init; } = new();
    public List<string> UncoveredTypes { get; init; } = new();
    public List<ApiTypeSummary> ApiTypes { get; init; } = new();
}

internal sealed class CoverageMetrics
{
    public int Total { get; init; }
    public int Covered { get; init; }
    public double Percentage { get; init; }
}

internal sealed class TypeCoverageInfo
{
    public required string FullName { get; init; }
    public string? CoveredBy { get; init; }
    public int MembersCovered { get; init; }
    public int MembersTotal { get; init; }
}

internal sealed class ApiTypeSummary
{
    public required string FullName { get; init; }
    public List<string> WrappedTypes { get; init; } = new();
}

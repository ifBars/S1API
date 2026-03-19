using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Tracks versions and detects changes between coverage runs.
/// </summary>
public sealed class VersionTracker
{
    /// <summary>
    /// Get the version of an assembly.
    /// </summary>
    public string GetAssemblyVersion(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Get combined versions for multiple assemblies.
    /// </summary>
    public string GetAssemblyVersion(IEnumerable<string> assemblyPaths)
    {
        return string.Join(", ",
            assemblyPaths
                .Select(Path.GetFileName)
                .Zip(assemblyPaths.Select(GetAssemblyVersion), (name, version) => $"{name}:{version}"));
    }
    
    /// <summary>
    /// Get a hash of an assembly file for change detection.
    /// </summary>
    public string GetAssemblyHash(string assemblyPath)
    {
        try
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(assemblyPath);
            var hash = sha256.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
        catch
        {
            // Fallback to file modification time if hash fails
            var fileInfo = new FileInfo(assemblyPath);
            return fileInfo.LastWriteTimeUtc.Ticks.ToString();
        }
    }

    /// <summary>
    /// Get a combined hash for multiple assembly files for change detection.
    /// </summary>
    public string GetAssemblyHash(IEnumerable<string> assemblyPaths)
    {
        var normalizedPaths = assemblyPaths
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            using var sha256 = SHA256.Create();
            var combined = string.Join("\n", normalizedPaths.Select(path => $"{Path.GetFileName(path)}:{GetAssemblyHash(path)}"));
            var bytes = Encoding.UTF8.GetBytes(combined);
            return Convert.ToHexString(sha256.ComputeHash(bytes));
        }
        catch
        {
            return string.Join("|", normalizedPaths.Select(GetAssemblyHash));
        }
    }
    
    /// <summary>
    /// Get the analyzer version (from the analyzer assembly itself).
    /// </summary>
    public string GetAnalyzerVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
    
    /// <summary>
    /// Detect changes between the previous entry and current coverage result.
    /// Returns a list of events that occurred.
    /// </summary>
    public List<HistoryEvent> DetectChanges(
        CoverageHistoryEntry? previous,
        CoverageResult current,
        IReadOnlyList<string> gameAssemblyPaths)
    {
        var events = new List<HistoryEvent>();
        
        if (previous == null)
        {
            // First entry - no changes to detect
            return events;
        }
        
        var currentAnalyzerVersion = GetAnalyzerVersion();
        var currentGameHash = GetAssemblyHash(gameAssemblyPaths);
        
        // 1. Detect analyzer update
        if (previous.AnalyzerVersion != currentAnalyzerVersion)
        {
            events.Add(new HistoryEvent
            {
                Type = EventType.AnalyzerUpdate,
                Description = $"Analyzer updated: {previous.AnalyzerVersion} → {currentAnalyzerVersion}",
                Details = "Coverage analyzer version changed"
            });
        }
        
        // 2. Detect game assembly update
        if (previous.GameAssemblyHash != currentGameHash)
        {
            var typeCountChange = current.TotalGameClasses - previous.TotalClasses;
            var percentChange = previous.TotalClasses > 0 
                ? (double)typeCountChange / previous.TotalClasses * 100 
                : 0.0;
            
            // Significant change threshold: 5% or 50+ types
            if (Math.Abs(percentChange) > 5.0 || Math.Abs(typeCountChange) > 50)
            {
                events.Add(new HistoryEvent
                {
                    Type = EventType.GameUpdate,
                    Description = $"Game updated: {typeCountChange:+#;-#;0} types ({percentChange:+0.0;-0.0}%)",
                    Details = $"Hash: {previous.GameAssemblyHash[..8]} → {currentGameHash[..8]}"
                });
            }
        }
        
        // 3. Detect coverage improvements
        if (previous != null)
        {
            var classCoverageChange = current.ClassCoveragePercentage - previous.ClassCoveragePercentage;
            var memberCoverageChange = current.MemberCoveragePercentage - previous.MemberCoveragePercentage;
            
            // Significant coverage increase (2%+ for classes, 1%+ for members)
            if (classCoverageChange > 2.0)
            {
                events.Add(new HistoryEvent
                {
                    Type = EventType.ApiExpansion,
                    Description = $"Class coverage increased by {classCoverageChange:+0.00}%",
                    Details = $"{previous.ClassCoveragePercentage:F2}% → {current.ClassCoveragePercentage:F2}%"
                });
            }
            else if (classCoverageChange > 0.5 && previous.AnalyzerVersion != currentAnalyzerVersion)
            {
                // Smaller increase but analyzer changed - likely matching improvement
                events.Add(new HistoryEvent
                {
                    Type = EventType.MatchingImproved,
                    Description = "Matching algorithm improved detection",
                    Details = $"Coverage increased by {classCoverageChange:+0.00}%"
                });
            }
            
            if (memberCoverageChange > 1.0)
            {
                events.Add(new HistoryEvent
                {
                    Type = EventType.ApiExpansion,
                    Description = $"Member coverage increased by {memberCoverageChange:+0.00}%",
                    Details = $"{previous.MemberCoveragePercentage:F2}% → {current.MemberCoveragePercentage:F2}%"
                });
            }
        }
        
        return events;
    }
}


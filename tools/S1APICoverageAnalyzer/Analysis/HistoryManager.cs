using System.Text.Json;
using S1APICoverageAnalyzer.Models;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Manages reading, writing, and appending to the coverage history file.
/// </summary>
public sealed class HistoryManager
{
    private readonly string _historyFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public HistoryManager(string historyFilePath)
    {
        _historyFilePath = historyFilePath;
    }
    
    /// <summary>
    /// Load the coverage history from file, or create a new one if it doesn't exist.
    /// </summary>
    public CoverageHistory LoadHistory()
    {
        if (!File.Exists(_historyFilePath))
        {
            return new CoverageHistory();
        }
        
        try
        {
            var json = File.ReadAllText(_historyFilePath);
            var history = JsonSerializer.Deserialize<CoverageHistory>(json, JsonOptions);
            return history ?? new CoverageHistory();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load history file: {ex.Message}");
            Console.WriteLine("Creating new history file.");
            return new CoverageHistory();
        }
    }
    
    /// <summary>
    /// Append a new entry to the history and save.
    /// </summary>
    /// <returns>True if the coverage percentage changed from the last entry, false otherwise.</returns>
    public bool AppendEntry(CoverageHistoryEntry entry)
    {
        var history = LoadHistory();
        
        // Check if coverage changed from the previous entry
        var latestEntry = history.LatestEntry;
        bool coverageChanged = latestEntry == null ||
            Math.Abs(latestEntry.ClassCoveragePercentage - entry.ClassCoveragePercentage) > 0.01 ||
            Math.Abs(latestEntry.MemberCoveragePercentage - entry.MemberCoveragePercentage) > 0.01;
        
        // Remove duplicate entries (same timestamp within 1 minute)
        history.Entries.RemoveAll(e => 
            Math.Abs((e.Timestamp - entry.Timestamp).TotalMinutes) < 1.0 &&
            e.GameAssemblyHash == entry.GameAssemblyHash &&
            e.AnalyzerVersion == entry.AnalyzerVersion);
        
        var entries = history.Entries.ToList();
        entries.Add(entry);
        
        // Sort by timestamp
        entries = entries
            .OrderBy(e => e.Timestamp)
            .ToList();
        
        // Create new history with sorted entries
        var updatedHistory = new CoverageHistory
        {
            GeneratedBy = history.GeneratedBy,
            Version = history.Version,
            Entries = entries
        };
        
        SaveHistory(updatedHistory);
        
        return coverageChanged;
    }
    
    /// <summary>
    /// Save the coverage history to file.
    /// </summary>
    public void SaveHistory(CoverageHistory history)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(history, JsonOptions);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to save history file: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Get the latest entry in the history.
    /// </summary>
    public CoverageHistoryEntry? GetLatestEntry()
    {
        var history = LoadHistory();
        return history.LatestEntry;
    }
    
    /// <summary>
    /// Get entries within a date range.
    /// </summary>
    public List<CoverageHistoryEntry> GetEntriesInRange(DateTime start, DateTime end)
    {
        var history = LoadHistory();
        return history.GetEntriesInRange(start, end);
    }
    
    /// <summary>
    /// Remove duplicate consecutive entries where coverage percentages are identical.
    /// Keeps the first entry of each unique coverage percentage.
    /// </summary>
    public int DeduplicateHistory()
    {
        var history = LoadHistory();
        
        if (history.Entries.Count == 0)
        {
            return 0;
        }
        
        var sortedEntries = history.Entries
            .OrderBy(e => e.Timestamp)
            .ToList();
        
        var deduplicated = new List<CoverageHistoryEntry>();
        CoverageHistoryEntry? lastEntry = null;
        int removedCount = 0;
        
        foreach (var entry in sortedEntries)
        {
            // Keep the entry if it's the first one or if coverage changed from the previous entry
            if (lastEntry == null ||
                Math.Abs(lastEntry.ClassCoveragePercentage - entry.ClassCoveragePercentage) > 0.01 ||
                Math.Abs(lastEntry.MemberCoveragePercentage - entry.MemberCoveragePercentage) > 0.01)
            {
                deduplicated.Add(entry);
                lastEntry = entry;
            }
            else
            {
                removedCount++;
            }
        }
        
        // Save the deduplicated history
        var updatedHistory = new CoverageHistory
        {
            GeneratedBy = history.GeneratedBy,
            Version = history.Version,
            Entries = deduplicated
        };
        
        SaveHistory(updatedHistory);
        
        return removedCount;
    }
}


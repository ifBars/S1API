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
    public void AppendEntry(CoverageHistoryEntry entry)
    {
        var history = LoadHistory();
        
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
}


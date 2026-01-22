using System.CommandLine;
using S1APICoverageAnalyzer.Analysis;
using S1APICoverageAnalyzer.Models;
using S1APICoverageAnalyzer.Output;

namespace S1APICoverageAnalyzer;

// S1API Coverage Analyzer - Analyzes Schedule 1 game assembly and S1API mono assembly to provide a coverage report

public class Program
{
    private sealed record AnalysisOptions(
        FileInfo GameAssembly,
        FileInfo ApiAssembly,
        FileInfo? Output,
        FileInfo? BadgeOutput,
        FileInfo? TextOutput,
        bool Verbose,
        FileInfo? HistoryFile,
        bool SkipHistory,
        string? Annotation,
        FileInfo? ChartOutput,
        string ChartFormat);
    
    public static async Task<int> Main(string[] args)
    {
        var gameAssemblyOption = new Option<FileInfo>(
            aliases: ["--game-assembly", "-g"],
            description: "Path to the game assembly (Assembly-CSharp.dll)")
        {
            IsRequired = true
        };
        
        var apiAssemblyOption = new Option<FileInfo>(
            aliases: ["--api-assembly", "-a"],
            description: "Path to the S1API assembly (S1API.dll)")
        {
            IsRequired = true
        };
        
        var outputOption = new Option<FileInfo?>(
            aliases: ["--output", "-o"],
            description: "Path to write the JSON coverage report");
        
        var badgeOutputOption = new Option<FileInfo?>(
            aliases: ["--badge-output", "-b"],
            description: "Path to write the badge markdown");
        
        var textOutputOption = new Option<FileInfo?>(
            aliases: ["--text-output", "-t"],
            description: "Path to write a plain text coverage report");
        
        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            description: "Show detailed output");
        
        var historyFileOption = new Option<FileInfo?>(
            aliases: ["--history-file"],
            description: "Path to coverage history JSON file (default: coverage-history.json)");
        
        var skipHistoryOption = new Option<bool>(
            aliases: ["--skip-history"],
            description: "Don't update history (for testing)");
        
        var annotationOption = new Option<string?>(
            aliases: ["--annotation"],
            description: "Add a custom note to this history entry");
        
        var chartOutputOption = new Option<FileInfo?>(
            aliases: ["--chart-output"],
            description: "Path to write chart markdown/URL");
        
        var chartFormatOption = new Option<string>(
            aliases: ["--chart-format"],
            description: "Chart output format: url, markdown, html, mermaid",
            getDefaultValue: () => "markdown");
        
        var rootCommand = new RootCommand("S1API Coverage Analyzer - Analyzes API coverage of Schedule One game types")
        {
            gameAssemblyOption,
            apiAssemblyOption,
            outputOption,
            badgeOutputOption,
            textOutputOption,
            verboseOption,
            historyFileOption,
            skipHistoryOption,
            annotationOption,
            chartOutputOption,
            chartFormatOption
        };
        
        rootCommand.SetHandler(async (context) =>
        {
            var options = new AnalysisOptions(
                context.ParseResult.GetValueForOption(gameAssemblyOption)!,
                context.ParseResult.GetValueForOption(apiAssemblyOption)!,
                context.ParseResult.GetValueForOption(outputOption),
                context.ParseResult.GetValueForOption(badgeOutputOption),
                context.ParseResult.GetValueForOption(textOutputOption),
                context.ParseResult.GetValueForOption(verboseOption),
                context.ParseResult.GetValueForOption(historyFileOption),
                context.ParseResult.GetValueForOption(skipHistoryOption),
                context.ParseResult.GetValueForOption(annotationOption),
                context.ParseResult.GetValueForOption(chartOutputOption),
                context.ParseResult.GetValueForOption(chartFormatOption) ?? "markdown");
            await RunAnalysis(options);
        });
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static async Task RunAnalysis(AnalysisOptions options)
    {
        var (gameAssemblyFile, apiAssemblyFile, outputFile, badgeOutputFile, textOutputFile, verbose,
            historyFile, skipHistory, annotation, chartOutput, chartFormat) = options;
        
        Console.WriteLine("S1API Coverage Analyzer");
        Console.WriteLine("=======================");
        Console.WriteLine();
        
        // Validate input files
        if (!gameAssemblyFile.Exists)
        {
            Console.Error.WriteLine($"Error: Game assembly not found: {gameAssemblyFile.FullName}");
            Environment.Exit(1);
        }
        
        if (!apiAssemblyFile.Exists)
        {
            Console.Error.WriteLine($"Error: API assembly not found: {apiAssemblyFile.FullName}");
            Environment.Exit(1);
        }
        
        Console.WriteLine($"Game Assembly: {gameAssemblyFile.FullName}");
        Console.WriteLine($"API Assembly:  {apiAssemblyFile.FullName}");
        Console.WriteLine();
        
        try
        {
            // Create shared load context with both assembly directories
            using var loadContext = new SharedLoadContext(
                gameAssemblyFile.FullName,
                apiAssemblyFile.FullName);
            
            // Load both assemblies through the shared context
            var gameAssembly = loadContext.LoadAssembly(gameAssemblyFile.FullName);
            var apiAssembly = loadContext.LoadAssembly(apiAssemblyFile.FullName);
            
            // Analyze game assembly
            Console.WriteLine("Analyzing game assembly...");
            var gameAnalyzer = new GameAssemblyAnalyzer(gameAssembly, gameAssemblyFile.FullName);
            var gameTypes = gameAnalyzer.ExtractGameTypes();
            var excludedTypeCount = gameAnalyzer.CountExcludedTypes();
            Console.WriteLine($"  Found {gameTypes.Count} eligible game types ({excludedTypeCount} excluded)");
            
            // Analyze API assembly
            Console.WriteLine("Analyzing S1API assembly...");
            var apiAnalyzer = new ApiAssemblyAnalyzer(apiAssembly, apiAssemblyFile.FullName);
            apiAnalyzer.Analyze();
            var wrappedTypes = apiAnalyzer.GetWrappedGameTypes();
            var accessedMembers = apiAnalyzer.GetAccessedMembers();
            var apiTypes = apiAnalyzer.GetApiTypes();
            Console.WriteLine($"  Found {wrappedTypes.Count} wrapped game types across {apiTypes.Count} API types");
            
            // Calculate coverage
            Console.WriteLine("Calculating coverage...");
            var calculator = new CoverageCalculator(
                gameTypes, 
                wrappedTypes, 
                accessedMembers, 
                apiTypes,
                excludedTypeCount);
            var result = calculator.Calculate();
            
            // Output summary
            Console.WriteLine();
            Console.WriteLine(ReportGenerator.GenerateConsoleSummary(result));
            
            // Generate badge URL
            var badgeUrl = BadgeGenerator.GenerateClassCoverageBadgeUrl(result);
            Console.WriteLine($"Badge URL: {badgeUrl}");
            Console.WriteLine();
            
            // History tracking
            if (!skipHistory)
            {
                var historyPath = historyFile?.FullName ?? 
                    Path.Combine(Path.GetDirectoryName(outputFile?.FullName ?? ".") ?? ".", "coverage-history.json");
                
                Console.WriteLine("Updating coverage history...");
                await UpdateCoverageHistory(historyPath, result, gameAssemblyFile.FullName, annotation, verbose);
                Console.WriteLine($"History updated: {historyPath}");
                Console.WriteLine();
            }
            
            // Write outputs
            if (outputFile != null)
            {
                // Ensure directory exists
                var dir = outputFile.DirectoryName;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                await ReportGenerator.WriteJsonReportAsync(result, outputFile.FullName);
                Console.WriteLine($"JSON report written to: {outputFile.FullName}");
            }
            
            if (badgeOutputFile != null)
            {
                var dir = badgeOutputFile.DirectoryName;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                await ReportGenerator.WriteBadgeMarkdownAsync(result, badgeOutputFile.FullName);
                Console.WriteLine($"Badge markdown written to: {badgeOutputFile.FullName}");
            }
            
            if (textOutputFile != null)
            {
                var dir = textOutputFile.DirectoryName;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                await ReportGenerator.WriteTextReportAsync(result, textOutputFile.FullName);
                Console.WriteLine($"Text report written to: {textOutputFile.FullName}");
            }
            
            // Chart generation
            if (chartOutput != null && !skipHistory)
            {
                var historyPath = historyFile?.FullName ?? 
                    Path.Combine(Path.GetDirectoryName(outputFile?.FullName ?? ".") ?? ".", "coverage-history.json");
                
                Console.WriteLine("Generating coverage chart...");
                await GenerateCoverageChart(historyPath, chartOutput.FullName, chartFormat, verbose);
                Console.WriteLine($"Chart written to: {chartOutput.FullName}");
            }
            
            // Verbose output
            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine("Covered Types:");
                foreach (var type in result.CoveredTypes.OrderBy(t => t.FullName))
                {
                    Console.WriteLine($"  [covered] {type.FullName}");
                    if (!string.IsNullOrEmpty(type.CoveredByApiType))
                        Console.WriteLine($"      -> Wrapped by: {type.CoveredByApiType}");
                }
                
                Console.WriteLine();
                Console.WriteLine("Uncovered Types (first 50):");
                foreach (var type in result.UncoveredTypes.OrderBy(t => t.FullName).Take(50))
                {
                    Console.WriteLine($"  [uncovered] {type.FullName}");
                }
                
                if (result.UncoveredTypes.Count > 50)
                    Console.WriteLine($"  ... and {result.UncoveredTypes.Count - 50} more");
            }
            
            Console.WriteLine();
            Console.WriteLine("Analysis complete!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during analysis: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
    
    private static Task UpdateCoverageHistory(
        string historyFilePath,
        CoverageResult result,
        string gameAssemblyPath,
        string? annotation,
        bool verbose)
    {
        try
        {
            var historyManager = new HistoryManager(historyFilePath);
            var versionTracker = new VersionTracker();
            
            var previousEntry = historyManager.GetLatestEntry();
            
            // Detect changes
            var events = versionTracker.DetectChanges(previousEntry, result, gameAssemblyPath);
            
            // Add manual annotation if provided
            if (!string.IsNullOrEmpty(annotation))
            {
                events.Add(new HistoryEvent
                {
                    Type = EventType.ManualAnnotation,
                    Description = annotation
                });
            }
            
            // Create new entry
            var entry = new CoverageHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                ClassCoveragePercentage = result.ClassCoveragePercentage,
                MemberCoveragePercentage = result.MemberCoveragePercentage,
                TotalClasses = result.TotalGameClasses,
                CoveredClasses = result.CoveredGameClasses,
                TotalMembers = result.TotalGameMembers,
                CoveredMembers = result.CoveredGameMembers,
                ExcludedClasses = result.ExcludedTypeCount,
                GameAssemblyVersion = versionTracker.GetAssemblyVersion(gameAssemblyPath),
                GameAssemblyHash = versionTracker.GetAssemblyHash(gameAssemblyPath),
                AnalyzerVersion = versionTracker.GetAnalyzerVersion(),
                Events = events,
                Note = annotation
            };
            
            // Append to history
            historyManager.AppendEntry(entry);
            
            if (verbose && events.Count > 0)
            {
                Console.WriteLine($"  Detected {events.Count} event(s):");
                foreach (var evt in events)
                {
                    Console.WriteLine($"    - [{evt.Type}] {evt.Description}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to update history: {ex.Message}");
            if (verbose)
                Console.WriteLine(ex.StackTrace);
        }
        
        return Task.CompletedTask;
    }
    
    private static async Task GenerateCoverageChart(
        string historyFilePath,
        string chartOutputPath,
        string chartFormat,
        bool verbose)
    {
        try
        {
            var historyManager = new HistoryManager(historyFilePath);
            var history = historyManager.LoadHistory();
            
            if (history.Entries.Count == 0)
            {
                Console.WriteLine("  No history data available yet. Run analysis a few times to build history.");
                return;
            }
            
            var chartGenerator = new ChartGenerator();
            
            // Ensure directory exists
            var dir = Path.GetDirectoryName(chartOutputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            switch (chartFormat.ToLowerInvariant())
            {
                case "url":
                    var chartUrl = chartGenerator.GenerateChartUrl(history, new ChartOptions());
                    await File.WriteAllTextAsync(chartOutputPath, chartUrl);
                    break;
                    
                case "markdown":
                    var markdown = chartGenerator.GenerateChartMarkdown(history, new ChartOptions());
                    await File.WriteAllTextAsync(chartOutputPath, markdown);
                    break;
                    
                case "html":
                    var html = chartGenerator.GenerateChartHtml(history, new ChartOptions());
                    await File.WriteAllTextAsync(chartOutputPath, html);
                    break;
                    
                case "mermaid":
                    var mermaid = chartGenerator.GenerateChartMermaid(history, new ChartOptions());
                    await File.WriteAllTextAsync(chartOutputPath, mermaid);
                    break;
                    
                default:
                    Console.WriteLine($"  Warning: Unknown chart format '{chartFormat}', using markdown");
                    var defaultMarkdown = chartGenerator.GenerateChartMarkdown(history, new ChartOptions());
                    await File.WriteAllTextAsync(chartOutputPath, defaultMarkdown);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to generate chart: {ex.Message}");
            if (verbose)
                Console.WriteLine(ex.StackTrace);
        }
    }
}

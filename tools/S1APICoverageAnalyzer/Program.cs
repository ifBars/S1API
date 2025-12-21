using System.CommandLine;
using S1APICoverageAnalyzer.Analysis;
using S1APICoverageAnalyzer.Output;

namespace S1APICoverageAnalyzer;

public class Program
{
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
        
        var rootCommand = new RootCommand("S1API Coverage Analyzer - Analyzes API coverage of Schedule One game types")
        {
            gameAssemblyOption,
            apiAssemblyOption,
            outputOption,
            badgeOutputOption,
            textOutputOption,
            verboseOption
        };
        
        rootCommand.SetHandler(async (gameAssembly, apiAssembly, output, badgeOutput, textOutput, verbose) =>
        {
            await RunAnalysis(gameAssembly, apiAssembly, output, badgeOutput, textOutput, verbose);
        }, gameAssemblyOption, apiAssemblyOption, outputOption, badgeOutputOption, textOutputOption, verboseOption);
        
        return await rootCommand.InvokeAsync(args);
    }
    
    private static async Task RunAnalysis(
        FileInfo gameAssemblyFile,
        FileInfo apiAssemblyFile,
        FileInfo? outputFile,
        FileInfo? badgeOutputFile,
        FileInfo? textOutputFile,
        bool verbose)
    {
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
}

using System.Reflection;

namespace S1APICoverageAnalyzer.Analysis;

/// <summary>
/// Creates and manages a shared MetadataLoadContext for loading multiple assemblies.
/// This ensures that assemblies are only loaded once and can reference each other.
/// </summary>
public sealed class SharedLoadContext : IDisposable
{
    private readonly MetadataLoadContext _loadContext;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Create a shared load context with the specified assembly search paths.
    /// The first path is considered the primary source; duplicate assembly names from
    /// later paths will be ignored.
    /// </summary>
    /// <param name="assemblyPaths">Paths to specific assemblies or directories to search for dependencies.</param>
    public SharedLoadContext(params string[] assemblyPaths)
    {
        var allPaths = CollectAssemblyPaths(assemblyPaths);
        var resolver = new PathAssemblyResolver(allPaths);
        _loadContext = new MetadataLoadContext(resolver);
    }

    /// <summary>
    /// Load an assembly from the specified path.
    /// Returns cached assembly if already loaded.
    /// </summary>
    public Assembly LoadAssembly(string assemblyPath)
    {
        var normalizedPath = Path.GetFullPath(assemblyPath);
        
        if (_loadedAssemblies.TryGetValue(normalizedPath, out var existing))
            return existing;
        
        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException($"Assembly not found: {normalizedPath}");
        
        var assembly = _loadContext.LoadFromAssemblyPath(normalizedPath);
        _loadedAssemblies[normalizedPath] = assembly;
        return assembly;
    }

    /// <summary>
    /// Collect all assembly paths from the specified paths.
    /// Deduplicates by assembly file name, keeping the first occurrence.
    /// </summary>
    private static HashSet<string> CollectAssemblyPaths(string[] paths)
    {
        // Track seen assembly names to avoid duplicates
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Helper to add a DLL only if we haven't seen this assembly name before
        void TryAddDll(string dllPath)
        {
            var fileName = Path.GetFileName(dllPath);
            if (seenNames.Add(fileName))
            {
                result.Add(dllPath);
            }
        }
        
        // Add runtime assemblies for core types first (highest priority)
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrEmpty(runtimeDir))
        {
            foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            {
                TryAddDll(dll);
            }
        }
        
        // Process each provided path in order (first has priority)
        foreach (var path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;
            
            if (File.Exists(path))
            {
                // It's a file - add it first, then other DLLs in its directory
                TryAddDll(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                    {
                        TryAddDll(dll);
                    }
                }
            }
            else if (Directory.Exists(path))
            {
                // It's a directory - add all DLLs in it
                foreach (var dll in Directory.GetFiles(path, "*.dll"))
                {
                    TryAddDll(dll);
                }
            }
        }
        
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _loadContext.Dispose();
        GC.SuppressFinalize(this);
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args.Length < 2)
    throw new ArgumentException("Expected baseline.dll, current.dll and optional dependency directories.");

var dependencies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator).Concat(args.Skip(2).SelectMany(dir => Directory.GetFiles(dir, "*.dll")))
    .Where(path => !Path.GetFileNameWithoutExtension(path).Equals("S1API", StringComparison.OrdinalIgnoreCase))
    .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
    .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

var before = ReadContracts(args[0]);
var after = ReadContracts(args[1]);
var changes = before.Keys.Intersect(after.Keys, StringComparer.Ordinal)
    .Where(id => before[id] != after[id]).Order(StringComparer.Ordinal).ToArray();
foreach (var id in changes)
    Console.WriteLine($"{id}\n  before: {before[id]}\n  after:  {after[id]}");
return changes.Length == 0 ? 0 : 2;

Dictionary<string, string> ReadContracts(string path)
{
    var reference = MetadataReference.CreateFromFile(Path.GetFullPath(path));
    var compilation = CSharpCompilation.Create("ApiConsumer", references: dependencies.Append(reference));
    var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!;
    var contracts = new Dictionary<string, string>(StringComparer.Ordinal);
    Visit(assembly.GlobalNamespace);
    if (contracts.Count == 0)
        throw new InvalidDataException($"No public API found in {path}.");
    return contracts;

    void Visit(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                Visit(ns);
                continue;
            }

            if (member.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
                continue;

            // Accessor methods expose the read and write contracts independently.
            var signature = member switch
            {
                IMethodSymbol method => string.Join(" | ", method.Parameters.Select(p => p.ToDisplayString(ParameterFormat()))
                    .Prepend(method.ReturnType.ToDisplayString(TypeFormat()))),
                IFieldSymbol field => field.Type.ToDisplayString(TypeFormat()),
                IEventSymbol evt => evt.Type.ToDisplayString(TypeFormat()),
                _ => null
            };
            if (signature != null && member.GetDocumentationCommentId() is string id)
                contracts.Add(id, signature);

            if (member is INamedTypeSymbol nested)
                Visit(nested);
        }
    }
}

static SymbolDisplayFormat TypeFormat() => SymbolDisplayFormat.FullyQualifiedFormat
    .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

static SymbolDisplayFormat ParameterFormat() => TypeFormat().WithParameterOptions(
    SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
    SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeParamsRefOut |
    SymbolDisplayParameterOptions.IncludeOptionalBrackets);

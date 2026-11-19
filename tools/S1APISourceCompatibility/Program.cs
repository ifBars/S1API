using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

if (args is ["--review", var baseline, var candidate, var output])
    return MaintainerReview.Compare(baseline, candidate, output);

if (args.Length < 2)
    throw new ArgumentException("Expected baseline.dll, current.dll and optional dependency directories.");

var dependencies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator).Concat(args.Skip(2).SelectMany(dir => Directory.GetFiles(dir, "*.dll")))
    .Where(path => !Path.GetFileNameWithoutExtension(path).Equals("S1API", StringComparison.OrdinalIgnoreCase))
    .DistinctBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
    .Select(path => MetadataReference.CreateFromFile(path)).ToArray();

var before = ReadContracts(args[0]);
var after = ReadContracts(args[1]);
var leaks = after.Where(pair => pair.Value.Native &&
    (!before.TryGetValue(pair.Key, out var old) || !old.Native)).OrderBy(p => p.Key).ToArray();
if (leaks.Length > 0)
{
    foreach (var leak in leaks)
        Console.WriteLine($"Native game type exposed by {leak.Key}: {leak.Value.Signature}");
    return 3;
}
var changes = before.Keys.Intersect(after.Keys, StringComparer.Ordinal)
    .Where(id => before[id] != after[id]).Order(StringComparer.Ordinal).ToArray();
foreach (var id in changes)
    Console.WriteLine($"{id}\n  before: {before[id].Signature}\n  after:  {after[id].Signature}");
return changes.Length == 0 ? 0 : 2;

static bool ContainsGameType(ITypeSymbol type)
{
    if (type is IArrayTypeSymbol array) return ContainsGameType(array.ElementType);
    if (type is IPointerTypeSymbol pointer) return ContainsGameType(pointer.PointedAtType);
    if (type is not INamedTypeSymbol named) return false;
    var ns = named.ContainingNamespace.ToDisplayString() + ".";
    return ns.StartsWith("ScheduleOne.", StringComparison.Ordinal) || ns.StartsWith("Il2CppScheduleOne.", StringComparison.Ordinal) ||
        named.TypeArguments.Any(ContainsGameType) || (named.ContainingType != null && ContainsGameType(named.ContainingType));
}

Dictionary<string, (string Signature, bool Native)> ReadContracts(string path)
{
    var reference = MetadataReference.CreateFromFile(Path.GetFullPath(path));
    var compilation = CSharpCompilation.Create("ApiConsumer", references: dependencies.Append(reference));
    var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference)!;
    var contracts = new Dictionary<string, (string Signature, bool Native)>(StringComparer.Ordinal);
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
                INamedTypeSymbol type => string.Join(" | ", TypeContracts(type).Select(t => t.ToDisplayString(TypeFormat()))
                    .Order(StringComparer.Ordinal)),
                _ => null
            };
            if (signature != null && member.GetDocumentationCommentId() is string id)
            {
                bool native = member switch
                {
                    IMethodSymbol m => ContainsGameType(m.ReturnType) || m.Parameters.Any(p => ContainsGameType(p.Type)) ||
                        m.TypeParameters.SelectMany(t => t.ConstraintTypes).Any(ContainsGameType),
                    IFieldSymbol f => ContainsGameType(f.Type),
                    IEventSymbol e => ContainsGameType(e.Type),
                    INamedTypeSymbol type => TypeContracts(type).Any(ContainsGameType),
                    _ => false
                };
                contracts.Add(id, (signature, native));
            }

            if (member is INamedTypeSymbol nested)
                Visit(nested);
        }
    }
}

static IEnumerable<ITypeSymbol> TypeContracts(INamedTypeSymbol type)
{
    if (type.BaseType != null) yield return type.BaseType;
    foreach (var contract in type.Interfaces) yield return contract;
    foreach (var constraint in type.TypeParameters.SelectMany(t => t.ConstraintTypes)) yield return constraint;
}

static SymbolDisplayFormat TypeFormat() => SymbolDisplayFormat.FullyQualifiedFormat
    .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

static SymbolDisplayFormat ParameterFormat() => TypeFormat().WithParameterOptions(
    SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
    SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeParamsRefOut |
    SymbolDisplayParameterOptions.IncludeOptionalBrackets);

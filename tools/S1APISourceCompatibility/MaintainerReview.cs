using System.Diagnostics;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class MaintainerReview
{
    private sealed record Finding(string rule, string path, int line);
    private sealed record Site(string Rule, string Key, int Line);

    public static int Compare(string baseline, string candidate, string output)
    {
        var findings = new List<Finding>();
        var paths = Git("diff", "--name-only", "-z", "--no-renames", "--diff-filter=AM", baseline, candidate, "--",
            "S1API", "S1APILoader", "S1API.Tests").Split('\0', StringSplitOptions.RemoveEmptyEntries);
        foreach (var path in paths)
        {
            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding("docs", path, 1));
                continue;
            }
            if (!path.EndsWith(".cs", StringComparison.Ordinal)) continue;
            var current = Git("show", $"{candidate}:{path}");
            var previous = Git("ls-tree", baseline, "--", path).Length == 0 ? "" : Git("show", $"{baseline}:{path}");
            foreach (var runtime in new[] { "MONOMELON", "IL2CPPMELON" })
            {
                var old = Sites(previous, path, runtime).CountByKey();
                foreach (var site in Sites(current, path, runtime))
                {
                    if (old.TryGetValue(site.Key, out int count) && count > 0)
                        old[site.Key] = count - 1;
                    else
                        findings.Add(new Finding(site.Rule, path, site.Line));
                }
            }
        }

        var distinct = findings.Distinct().ToArray();
        File.WriteAllText(output, JsonSerializer.Serialize(distinct));
        foreach (var finding in distinct)
            Console.WriteLine($"{finding.path}:{finding.line} {finding.rule}");
        return distinct.Any(f => f.rule is "frame-log" or "startup-budget") ? 1 : 0;
    }

    private static Dictionary<string, int> CountByKey(this IEnumerable<Site> sites) =>
        sites.GroupBy(site => site.Key).ToDictionary(group => group.Key, group => group.Count());

    private static IEnumerable<Site> Sites(string source, string path, string runtime)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: new[] { runtime })).GetRoot();
        bool tests = path.StartsWith("S1API.Tests/", StringComparison.Ordinal);
        foreach (var trivia in root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
            yield return new Site("scope", "comment|" + trivia.ToString(), trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        foreach (var node in root.DescendantNodes())
        {
            var method = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            string owner = string.Join(".", node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Reverse()
                .Select(t => t.Identifier.ValueText).Append(method?.Identifier.ValueText ?? ""));
            string? rule = node switch
            {
                InvocationExpressionSyntax call => CallRule(call, method, tests),
                BasePropertyDeclarationSyntax p when !tests && IsVisible(p.Modifiers) => "public-api",
                BaseMethodDeclarationSyntax m when !tests && IsVisible(m.Modifiers) => "public-api",
                BaseFieldDeclarationSyntax f when !tests && IsVisible(f.Modifiers) => "public-api",
                BaseTypeDeclarationSyntax t when !tests && IsVisible(t.Modifiers) => "public-api",
                MethodDeclarationSyntax m when !tests && Matches(m.Identifier.ValueText, "Save", "Serialize", "Deserialize", "Network", "Rpc") => "persistence",
                MethodDeclarationSyntax m when !tests && Matches(m.Identifier.ValueText, "Cache", "Scan", "Benchmark") => "performance",
                _ => null
            };
            if (rule == null)
                continue;
            if (rule == "public-api" && node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any(t => !IsVisible(t.Modifiers)))
                continue;
            // Ignore formatting and method bodies when comparing declaration signatures.
            var tokens = node.ChildNodesAndTokens().Where(n => !n.IsNode || n.AsNode() is not
                (BlockSyntax or ArrowExpressionClauseSyntax or AccessorListSyntax or MemberDeclarationSyntax));
            var key = string.Join(" ", tokens.SelectMany(n => n.IsToken ? new[] { n.AsToken() }.AsEnumerable() : n.AsNode()!.DescendantTokens())
                .Select(t => t.Text));
            yield return new Site(rule, $"{rule}|{owner}|{key}", node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        }
    }

    private static bool IsVisible(SyntaxTokenList modifiers) =>
        modifiers.Any(SyntaxKind.PublicKeyword) || modifiers.Any(SyntaxKind.ProtectedKeyword);

    private static bool Matches(string name, params string[] parts) =>
        parts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static string? CallRule(InvocationExpressionSyntax call, MethodDeclarationSyntax? method, bool tests)
    {
        if (call.Expression is not MemberAccessExpressionSyntax access)
            return null;
        var name = access.Name.Identifier.ValueText;
        if (tests)
            return name is "GetMethod" or "GetProperty" ? "test-signal" : null;
        var receiver = access.Expression.ToString();
        var logger = receiver.Contains("logger", StringComparison.OrdinalIgnoreCase) || receiver == "Log";
        if (logger && name is ("Msg" or "MsgPastel" or "Warning" or "Error" or "BigError"))
        {
            if (method?.Body == null)
                return "diagnostic";
            var methodName = method.Identifier.ValueText;
            bool info = name is "Msg" or "MsgPastel";
            // A direct call before any branch or return in a Unity frame callback runs every invocation.
            var statement = call.Parent as ExpressionStatementSyntax;
            var straightLine = method.Body.Statements.TakeWhile(s => s is ExpressionStatementSyntax).ToArray();
            bool direct = statement != null && straightLine.Contains(statement);
            var declaringType = method.Parent as TypeDeclarationSyntax;
            bool frameCallback = declaringType?.BaseList?.Types.Any(t =>
                t.Type.ToString() is "MonoBehaviour" or "UnityEngine.MonoBehaviour") == true &&
                method.ParameterList.Parameters.Count == 0 && methodName is ("Update" or "FixedUpdate" or "LateUpdate");
            bool knownSink = receiver is "MelonLogger" or "MelonLoader.MelonLogger" ||
                declaringType?.Members.OfType<FieldDeclarationSyntax>().Any(f =>
                    f.Declaration.Type.ToString() is ("Log" or "S1API.Logging.Log" or "MelonLogger.Instance") &&
                    f.Declaration.Variables.Any(v => v.Identifier.ValueText == receiver)) == true;
            if (direct && knownSink && frameCallback)
                return "frame-log";
            if (direct && knownSink && info && method.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                methodName is ("OnInitializeMelon" or "OnApplicationEarlyStart") &&
                straightLine.Take(Array.IndexOf(straightLine, statement!) + 1).OfType<ExpressionStatementSyntax>()
                    .Count(s => s.Expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax m } &&
                        m.Name.Identifier.ValueText is ("Msg" or "MsgPastel") &&
                        m.Expression.ToString() == receiver) > 3)
                return "startup-budget";
            if (call.Ancestors().OfType<IfStatementSyntax>().Any(i => i.Condition.ToString().Contains("EnableVerboseLogging", StringComparison.Ordinal)))
                return "diagnostic";
            return info ? "console-info" : "diagnostic";
        }
        if (name is "GetField" or "GetProperty" or "GetMethod" or "FindObjectOfType" or "FindObjectsOfType")
            return "lookup";
        if (name is "Patch" or "Instantiate" or "AddComponent" or "StartCoroutine" or "Spawn" or "Destroy")
            return "lifecycle";
        if (Matches(name, "Serialize", "Deserialize", "ServerRpc", "ObserversRpc", "SaveData"))
            return "persistence";
        if (Matches(name, "Cache", "Benchmark", "Stopwatch") || receiver == "Stopwatch")
            return "performance";
        return null;
    }

    private static string Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {arguments[0]} failed.");
        return result;
    }
}

using System.Reflection;
using HarmonyLib;

namespace S1API.Tests.Internal.Patches;

public sealed class HarmonyPatchTargetTests
{
    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
        BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void AttributePatchesNameExactlyOneGameMethod()
    {
        // A patch named only by method name fails to bind when the type has several methods of that name, and
        // Harmony then skips its whole patch class. Compatibility tools can add overloads to the game assembly
        // (Polyfill adds GeneratePackagingIcon(string, string)), so name the signature wherever there is a choice.
        // Covers attribute-declared methods only: classes with TargetMethod(s) and getters/setters/constructors are skipped.
        List<string> problems = FindUnresolvablePatchTargets();
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static List<string> FindUnresolvablePatchTargets()
    {
        var problems = new List<string>();
        foreach (Type patchClass in AllTypes(typeof(global::S1API.Internal.Patches.NPCPatches).Assembly))
        {
            List<HarmonyMethod> classInfo = HarmonyMethodExtensions.GetFromType(patchClass);
            if (classInfo.Count == 0 ||
                patchClass.GetMethod("TargetMethod", AllDeclared) != null ||
                patchClass.GetMethod("TargetMethods", AllDeclared) != null)
            {
                continue;
            }

            foreach (MethodInfo patch in patchClass.GetMethods(AllDeclared))
            {
                List<HarmonyMethod> methodInfo = HarmonyMethodExtensions.GetFromMethod(patch);
                if (methodInfo.Count == 0)
                    continue;

                HarmonyMethod info = HarmonyMethod.Merge(classInfo.Concat(methodInfo).ToList());
                if (info.declaringType == null || info.methodName == null ||
                    (info.methodType != null && info.methodType != MethodType.Normal))
                {
                    continue;
                }

                string target = $"{info.declaringType.FullName}.{info.methodName}";
                int matches = CountMatches(info);
                if (matches != 1)
                {
                    problems.Add(
                        $"{patchClass.FullName}.{patch.Name} -> {target}: " +
                        (matches == 0 ? "no such method" : $"{matches} methods match"));
                }
            }
        }

        return problems;
    }

    private static int CountMatches(HarmonyMethod info)
    {
        // HarmonyX folds ref/out/pointer variations into argumentTypes when it reads the attribute.
        Type[]? wanted = info.argumentTypes;
        for (Type? type = info.declaringType; type != null; type = type.BaseType)
        {
            MethodInfo[] named = type.GetMethods(AllDeclared)
                .Where(m => m.Name == info.methodName)
                .ToArray();
            if (named.Length == 0)
                continue;

            return wanted == null
                ? named.Length
                : named.Count(m => m.GetParameters().Select(p => p.ParameterType).SequenceEqual(wanted));
        }

        return 0;
    }

    private static IEnumerable<Type> AllTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }
}

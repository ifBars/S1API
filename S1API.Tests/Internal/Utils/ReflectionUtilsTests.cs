using S1API.Internal.Utils;
using S1API.Logging;
using System.Reflection;
using System.Reflection.Emit;

namespace S1API.Tests.Internal.Utils;

public sealed class ReflectionUtilsTests
{
    [Fact]
    public void InstanceAccessSupportsMonoFieldAndIl2CppPropertyShapes()
    {
        var mono = new MonoShape();
        var il2Cpp = new Il2CppShape();

        Assert.True(ReflectionUtils.TrySetFieldOrProperty(mono, "runtimeMember", 14));
        Assert.True(ReflectionUtils.TrySetFieldOrProperty(il2Cpp, "runtimeMember", 14));
        Assert.Equal(14, ReflectionUtils.TryGetFieldOrProperty(mono, "runtimeMember"));
        Assert.Equal(14, ReflectionUtils.TryGetFieldOrProperty(il2Cpp, "runtimeMember"));
    }

    [Fact]
    public void StaticAccessSupportsMonoFieldAndIl2CppPropertyShapes()
    {
        ReflectionUtils.TrySetStaticFieldOrProperty(typeof(MonoStaticShape), "RuntimeMember", 31);
        ReflectionUtils.TrySetStaticFieldOrProperty(typeof(Il2CppStaticShape), "RuntimeMember", 31);

        Assert.Equal(31, ReflectionUtils.TryGetStaticFieldOrProperty(typeof(MonoStaticShape), "RuntimeMember"));
        Assert.Equal(31, ReflectionUtils.TryGetStaticFieldOrProperty(typeof(Il2CppStaticShape), "RuntimeMember"));
    }

    [Fact]
    public void StaticAccessWalksBaseTypesForNonPublicMembers()
    {
        ReflectionUtils.TrySetStaticFieldOrProperty(typeof(DerivedStaticShape), "RuntimeMember", 47);

        Assert.Equal(
            47,
            ReflectionUtils.TryGetStaticFieldOrProperty(typeof(DerivedStaticShape), "RuntimeMember"));
    }

    [Fact]
    public void DerivedTypeScanIncludesAssembliesThatReferenceTheBaseAssembly()
    {
        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        Assert.True(ReflectionUtils.CanContainTypesDerivedFrom(
            typeof(ReflectionUtilsTests).Assembly,
            typeof(ReflectionUtils).Assembly,
            loadedAssemblies));
    }

    [Fact]
    public void GetDerivedClassesFindsTypesInReferencingAssemblies()
    {
        Assert.Contains(
            typeof(DerivedLogShape),
            ReflectionUtils.GetDerivedClasses<Log>());
    }

    [Fact]
    public void DerivedTypeScanExcludesAssembliesWithoutAReferencePathToTheBaseAssembly()
    {
        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        Assert.False(ReflectionUtils.CanContainTypesDerivedFrom(
            typeof(string).Assembly,
            typeof(ReflectionUtils).Assembly,
            loadedAssemblies));
    }

    [Fact]
    public void DerivedTypeScanFollowsTransitiveAssemblyReferences()
    {
        var assemblyName = new AssemblyName($"S1API.ReflectionUtilsTests.Dynamic.{Guid.NewGuid():N}");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        moduleBuilder.DefineType(
                "DynamicReflectionCandidate",
                TypeAttributes.Public,
                typeof(ReflectionCandidateBridge))
            .CreateType();

        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        Assert.True(ReflectionUtils.CanContainTypesDerivedFrom(
            assemblyBuilder,
            typeof(ReflectionUtils).Assembly,
            loadedAssemblies));
    }

    [Fact]
    public void DerivedTypeScanDoesNotFollowSameNameAssembliesWithDifferentIdentities()
    {
        string assemblyName = $"S1API.ReflectionUtilsTests.Duplicate.{Guid.NewGuid():N}";
        AssemblyBuilder unrelatedAssembly = CreateDynamicAssembly(assemblyName, new Version(1, 0, 0, 0));
        Type unrelatedType = unrelatedAssembly
            .DefineDynamicModule(assemblyName)
            .DefineType("UnrelatedType", TypeAttributes.Public)
            .CreateType()!;

        AssemblyBuilder relatedAssembly = CreateDynamicAssembly(assemblyName, new Version(2, 0, 0, 0));
        relatedAssembly
            .DefineDynamicModule(assemblyName)
            .DefineType("RelatedType", TypeAttributes.Public, typeof(ReflectionCandidateBridge))
            .CreateType();

        AssemblyBuilder candidateAssembly = CreateDynamicAssembly(
            $"S1API.ReflectionUtilsTests.Candidate.{Guid.NewGuid():N}",
            new Version(1, 0, 0, 0));
        ModuleBuilder candidateModule = candidateAssembly.DefineDynamicModule(candidateAssembly.GetName().Name!);
        candidateModule
            .DefineType("CandidateType", TypeAttributes.Public, unrelatedType)
            .CreateType();

        Assert.False(ReflectionUtils.CanContainTypesDerivedFrom(
            candidateAssembly,
            typeof(ReflectionUtils).Assembly,
            AppDomain.CurrentDomain.GetAssemblies()));
    }

    [Theory]
    [InlineData("S1API, Version=3.0.1.0, Culture=neutral, PublicKeyToken=null")]
    [InlineData("S1API, Version=2.9.2.0, Culture=neutral, PublicKeyToken=null")]
    [InlineData("S1API, Version=3.2.1.0, Culture=neutral, PublicKeyToken=null")]
    [InlineData("s1api, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public void AReferenceToAnyVersionOfTheBaseAssemblyBindsToIt(string reference)
    {
        var loaded = new AssemblyName("S1API, Version=3.2.1.0, Culture=neutral, PublicKeyToken=null");

        Assert.True(ReflectionUtils.ReferenceBindsToDefinition(new AssemblyName(reference), loaded));
    }

    [Fact]
    public void AReferenceToADifferentAssemblyNameDoesNotBind()
    {
        var loaded = new AssemblyName("S1API, Version=3.2.1.0, Culture=neutral, PublicKeyToken=null");

        Assert.False(ReflectionUtils.ReferenceBindsToDefinition(
            new AssemblyName("S1APILoader, Version=3.2.1.0, Culture=neutral, PublicKeyToken=null"),
            loaded));
    }

    [Fact]
    public void AReferenceWithADifferentPublicKeyTokenDoesNotBind()
    {
        var signed = new AssemblyName("Example, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        var unsigned = new AssemblyName("Example, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        Assert.False(ReflectionUtils.ReferenceBindsToDefinition(signed, unsigned));
        Assert.False(ReflectionUtils.ReferenceBindsToDefinition(unsigned, signed));
        Assert.True(ReflectionUtils.ReferenceBindsToDefinition(signed, signed));
    }

    [Fact]
    public void ALoadedAssemblyOfADifferentVersionIsFollowedOnlyWhenItIsTheOnlyOneOfThatName()
    {
        var reference = new AssemblyName("Library, Version=1.5.0.0, Culture=neutral, PublicKeyToken=null");
        var newer = new AssemblyName("Library, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null");

        Assert.True(ReflectionUtils.ShouldFollowLoadedReference(newer, reference, loadedAssembliesWithThatName: 1));
        Assert.False(ReflectionUtils.ShouldFollowLoadedReference(newer, reference, loadedAssembliesWithThatName: 2));
    }

    [Fact]
    public void ALoadedAssemblyWithTheExactIdentityIsAlwaysFollowed()
    {
        var reference = new AssemblyName("Library, Version=1.5.0.0, Culture=neutral, PublicKeyToken=null");

        Assert.True(ReflectionUtils.ShouldFollowLoadedReference(reference, reference, loadedAssembliesWithThatName: 2));
    }

    private static AssemblyBuilder CreateDynamicAssembly(string name, Version version)
    {
        var assemblyName = new AssemblyName(name)
        {
            Version = version
        };

        return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    }

    private sealed class MonoShape
    {
#pragma warning disable CS0169
        private int runtimeMember;
#pragma warning restore CS0169
    }

    private sealed class Il2CppShape
    {
        public int runtimeMember { get; set; }
    }

    private static class MonoStaticShape
    {
#pragma warning disable CS0649
        public static int RuntimeMember;
#pragma warning restore CS0649
    }

    private static class Il2CppStaticShape
    {
        public static int RuntimeMember { get; set; }
    }

    private class BaseStaticShape
    {
#pragma warning disable CS0169, CS0649
        private static int RuntimeMember;
#pragma warning restore CS0169, CS0649
    }

    private sealed class DerivedStaticShape : BaseStaticShape
    {
    }

    public class ReflectionCandidateBridge
    {
    }

    private sealed class DerivedLogShape : Log
    {
        public DerivedLogShape()
            : base(nameof(DerivedLogShape))
        {
        }
    }
}

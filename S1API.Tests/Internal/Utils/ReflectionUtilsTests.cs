using S1API.Internal.Utils;

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
}

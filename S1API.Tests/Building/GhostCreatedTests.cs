using System.Reflection;
using S1API.Building;

#if IL2CPPMELON
using S1Building = Il2CppScheduleOne.Building;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1Building = ScheduleOne.Building;
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

namespace S1API.Tests.Building;

[Collection("BuildEvents")]
public sealed class GhostCreatedTests
{
    [Theory]
    [InlineData(typeof(S1Building.BuildStart_Grid))]
    [InlineData(typeof(S1Building.BuildStart_Surface))]
    [InlineData(typeof(S1Building.BuildStart_ProceduralGrid))]
    public void GhostPatchTargetsExistInTargetRuntime(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Assert.NotNull(type.GetMethod("CreateGhostModel", flags, null, new[] { typeof(S1ItemFramework.BuildableItemDefinition) }, null));
    }

    [Fact]
    public void GhostArgsCarryTheDefinitionIdAndGhost()
    {
        var args = new GhostCreatedEventArgs("my_folder", null!);

        Assert.Equal("my_folder", args.ItemId);
        Assert.Null(args.GameObject);
    }

    [Fact]
    public void ThrowingHandlerDoesNotStopTheOthers()
    {
        var seen = new List<string?>();
        var errors = new List<string>();
        BuildEvents.ErrorSinkForTesting = errors.Add;
        Action<GhostCreatedEventArgs> bad = _ => throw new InvalidOperationException("boom");
        Action<GhostCreatedEventArgs> good = a => seen.Add(a.ItemId);
        BuildEvents.OnGhostCreated += bad;
        BuildEvents.OnGhostCreated += good;
        try
        {
            BuildEvents.RaiseGhostCreated(new GhostCreatedEventArgs("rack", null!));
        }
        finally
        {
            BuildEvents.OnGhostCreated -= bad;
            BuildEvents.OnGhostCreated -= good;
            BuildEvents.ErrorSinkForTesting = null;
        }

        Assert.Equal(new[] { "rack" }, seen);
        Assert.Single(errors);
        Assert.Contains("boom", errors[0]);
    }

    [Fact]
    public void RaiseWithoutHandlersDoesNothing()
    {
        BuildEvents.RaiseGhostCreated(new GhostCreatedEventArgs("rack", null!));
    }
}

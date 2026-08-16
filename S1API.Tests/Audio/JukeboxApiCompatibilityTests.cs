using System.Reflection;
using S1API.Audio;
using UnityEngine;

namespace S1API.Tests.Audio;

public sealed class JukeboxApiCompatibilityTests
{
    [Fact]
    public void WrapperExposesTheExpectedManagedContract()
    {
        Assert.True(typeof(Jukebox).IsSealed);
        Assert.Empty(typeof(Jukebox).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        AssertProperty(nameof(Jukebox.GUID), typeof(string));
        AssertProperty(nameof(Jukebox.GameObject), typeof(GameObject));
        AssertProperty(nameof(Jukebox.Tracks), typeof(IReadOnlyList<JukeboxTrack>));
        AssertProperty(nameof(Jukebox.CurrentTrack), typeof(JukeboxTrack));
        AssertProperty(nameof(Jukebox.Volume), typeof(int));
        AssertProperty(nameof(Jukebox.NormalizedVolume), typeof(float));
        AssertProperty(nameof(Jukebox.IsPlaying), typeof(bool));
        AssertProperty(nameof(Jukebox.CurrentTrackTime), typeof(float));
        AssertProperty(nameof(Jukebox.CurrentTrackOrderIndex), typeof(int));
        AssertProperty(nameof(Jukebox.Shuffle), typeof(bool));
        AssertProperty(nameof(Jukebox.RepeatMode), typeof(JukeboxRepeatMode));
        AssertProperty(nameof(Jukebox.Sync), typeof(bool));
        AssertProperty(nameof(Jukebox.State), typeof(JukeboxState));

        EventInfo stateChanged = typeof(Jukebox).GetEvent(nameof(Jukebox.OnStateChanged))!;
        Assert.Equal(typeof(Action<JukeboxState>), stateChanged.EventHandlerType);

        AssertMethod(nameof(Jukebox.TogglePlay));
        AssertMethod(nameof(Jukebox.PreviousTrack));
        AssertMethod(nameof(Jukebox.NextTrack));
        AssertMethod(nameof(Jukebox.ChangeVolume), typeof(int));
        AssertMethod(nameof(Jukebox.SetVolume), typeof(int));
        AssertMethod(nameof(Jukebox.ToggleShuffle));
        AssertMethod(nameof(Jukebox.ToggleRepeatMode));
        AssertMethod(nameof(Jukebox.ToggleSync));
        AssertMethod(nameof(Jukebox.SelectTrack), typeof(int));
    }

    [Fact]
    public void DiscoveryExposesTheExpectedManagedContract()
    {
        MethodInfo fromGameObject = typeof(Jukebox).GetMethod(
            nameof(Jukebox.FromGameObject),
            new[] { typeof(GameObject) })!;
        Assert.True(fromGameObject.IsStatic);
        Assert.Equal(typeof(Jukebox), fromGameObject.ReturnType);

        MethodInfo getAll = typeof(JukeboxManager).GetMethod(
            nameof(JukeboxManager.GetAll),
            Type.EmptyTypes)!;
        Assert.Equal(typeof(IReadOnlyList<Jukebox>), getAll.ReturnType);

        MethodInfo getByGuid = typeof(JukeboxManager).GetMethod(
            nameof(JukeboxManager.GetByGUID),
            new[] { typeof(string) })!;
        Assert.Equal(typeof(Jukebox), getByGuid.ReturnType);
    }

    [Fact]
    public void SnapshotTypesRemainSealedAndInternallyConstructed()
    {
        foreach (Type type in new[] { typeof(JukeboxTrack), typeof(JukeboxState) })
        {
            Assert.True(type.IsSealed);
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        }
    }

    [Theory]
    [InlineData(typeof(JukeboxTrack), "Index", typeof(int))]
    [InlineData(typeof(JukeboxTrack), "Name", typeof(string))]
    [InlineData(typeof(JukeboxTrack), "Artist", typeof(string))]
    [InlineData(typeof(JukeboxState), "Volume", typeof(int))]
    [InlineData(typeof(JukeboxState), "NormalizedVolume", typeof(float))]
    [InlineData(typeof(JukeboxState), "IsPlaying", typeof(bool))]
    [InlineData(typeof(JukeboxState), "CurrentTrackTime", typeof(float))]
    [InlineData(typeof(JukeboxState), "CurrentTrackOrderIndex", typeof(int))]
    [InlineData(typeof(JukeboxState), "Shuffle", typeof(bool))]
    [InlineData(typeof(JukeboxState), "RepeatMode", typeof(JukeboxRepeatMode))]
    [InlineData(typeof(JukeboxState), "Sync", typeof(bool))]
    [InlineData(typeof(JukeboxState), "CurrentTrack", typeof(JukeboxTrack))]
    public void SnapshotPropertiesRemainReadOnly(Type type, string propertyName, Type propertyType)
    {
        PropertyInfo property = type.GetProperty(propertyName)!;
        Assert.Equal(propertyType, property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    private static void AssertProperty(string propertyName, Type propertyType)
    {
        PropertyInfo property = typeof(Jukebox).GetProperty(propertyName)!;
        Assert.Equal(propertyType, property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    private static void AssertMethod(string methodName, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(Jukebox).GetMethod(methodName, parameterTypes)!;
        Assert.Equal(typeof(void), method.ReturnType);
    }
}

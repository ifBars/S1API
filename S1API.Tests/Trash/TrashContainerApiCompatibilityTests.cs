using System.Reflection;
using S1API.Trash;

namespace S1API.Tests.Trash;

public sealed class TrashContainerApiCompatibilityTests
{
    [Fact]
    public void WrapperSurfaceUsesManagedTypes()
    {
        Assert.True(typeof(TrashContainer).IsSealed);
        Assert.Equal(typeof(int), GetProperty(nameof(TrashContainer.Capacity)).PropertyType);
        Assert.Equal(typeof(int), GetProperty(nameof(TrashContainer.Level)).PropertyType);
        Assert.Equal(typeof(float), GetProperty(nameof(TrashContainer.NormalizedLevel)).PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<TrashContentEntry>),
            GetProperty(nameof(TrashContainer.Contents)).PropertyType);
        Assert.Equal(typeof(bool), GetProperty(nameof(TrashContainer.CanBeBagged)).PropertyType);
        Assert.Equal(typeof(Action<string>), GetEvent(nameof(TrashContainer.OnTrashAdded)).EventHandlerType);
        Assert.Equal(typeof(Action), GetEvent(nameof(TrashContainer.OnTrashLevelChanged)).EventHandlerType);
        Assert.Equal(typeof(bool), GetMethod(nameof(TrashContainer.TryBagTrash)).ReturnType);
    }

    [Fact]
    public void ContentEntryIsAnImmutableManagedValue()
    {
        Assert.True(typeof(TrashContentEntry).IsValueType);
        Assert.True(typeof(TrashContentEntry).IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute)));

        PropertyInfo[] properties = typeof(TrashContentEntry).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(4, properties.Length);
        Assert.All(properties, property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(
            typeof(TrashContentEntry).GetFields(BindingFlags.Instance | BindingFlags.Public),
            field => !field.IsInitOnly);
    }

    [Fact]
    public void ConstructionDoesNotExposeNativeTypes()
    {
        Assert.Empty(typeof(TrashContainer).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
        Assert.Empty(typeof(TrashContentEntry).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }

    private static PropertyInfo GetProperty(string name) =>
        typeof(TrashContainer).GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Missing public property {name}.");

    private static EventInfo GetEvent(string name) =>
        typeof(TrashContainer).GetEvent(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Missing public event {name}.");

    private static MethodInfo GetMethod(string name) =>
        typeof(TrashContainer).GetMethod(name, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Missing public method {name}.");
}

using System.Reflection;
using System.Runtime.CompilerServices;
using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

public sealed class BuildableGhostApiCompatibilityTests
{
    [Fact]
    public void BuildableBuilderExposesOptInGhostVisualFactory()
    {
        MethodInfo? method = typeof(BuildableItemDefinitionBuilder).GetMethod(
            nameof(BuildableItemDefinitionBuilder.WithGhostVisual),
            new[] { typeof(Func<Transform, GameObject>), typeof(bool) });

        Assert.NotNull(method);
        Assert.Equal(typeof(BuildableItemDefinitionBuilder), method!.ReturnType);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal("visualFactory", parameters[0].Name);
        Assert.Equal("replaceExistingVisual", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);
        Assert.Equal(false, parameters[1].DefaultValue);
    }

    [Fact]
    public void GhostVisualFactoryRejectsNull()
    {
        var builder = (BuildableItemDefinitionBuilder)RuntimeHelpers.GetUninitializedObject(
            typeof(BuildableItemDefinitionBuilder));

        Assert.Throws<ArgumentNullException>(() => builder.WithGhostVisual(null!));
    }
}

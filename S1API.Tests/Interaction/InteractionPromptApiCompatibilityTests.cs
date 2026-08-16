using System.Reflection;
using S1API.Interaction;
using UnityEngine;

namespace S1API.Tests.Interaction;

public sealed class InteractionPromptApiCompatibilityTests
{
    [Fact]
    public void BuilderFactoriesPreserveSourceAndBinaryShape()
    {
        AssertStaticFactory(
            typeof(InteractionPromptBuilder),
            nameof(InteractionPromptBuilder.Create));
        AssertStaticFactory(
            typeof(InteractionPrompt),
            nameof(InteractionPrompt.CreateBuilder));
    }

    [Fact]
    public void BuilderExposesExpectedFluentShape()
    {
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithMessage), typeof(string));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithInput), typeof(InteractionPromptInput));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithState), typeof(InteractionPromptState));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithRange), typeof(float));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithPriority), typeof(int));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithAngleLimit), typeof(float));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithoutAngleLimit));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithDisplayLocation), typeof(Transform));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.WithDisplayLocation), typeof(Collider));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.OnHovered), typeof(Action));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.OnInteractionStarted), typeof(Action));
        AssertBuilderMethod(nameof(InteractionPromptBuilder.OnInteractionEnded), typeof(Action));

        MethodInfo build = typeof(InteractionPromptBuilder).GetMethod(
            nameof(InteractionPromptBuilder.Build),
            Type.EmptyTypes)!;
        Assert.Equal(typeof(InteractionPrompt), build.ReturnType);
    }

    [Fact]
    public void HandleExposesExpectedRuntimeShape()
    {
        AssertHandleMethod(nameof(InteractionPrompt.SetMessage), typeof(string));
        AssertHandleMethod(nameof(InteractionPrompt.SetInput), typeof(InteractionPromptInput));
        AssertHandleMethod(nameof(InteractionPrompt.SetState), typeof(InteractionPromptState));
        AssertHandleMethod(nameof(InteractionPrompt.SetRange), typeof(float));
        AssertHandleMethod(nameof(InteractionPrompt.SetPriority), typeof(int));
        AssertHandleMethod(nameof(InteractionPrompt.SetAngleLimit), typeof(float));
        AssertHandleMethod(nameof(InteractionPrompt.WithoutAngleLimit));
        AssertHandleMethod(nameof(InteractionPrompt.SetDisplayLocation), typeof(Transform));
        AssertHandleMethod(nameof(InteractionPrompt.SetDisplayLocation), typeof(Collider));
        AssertHandleMethod(nameof(InteractionPrompt.ClearDisplayLocation));

        MethodInfo remove = typeof(InteractionPrompt).GetMethod(
            nameof(InteractionPrompt.Remove),
            Type.EmptyTypes)!;
        Assert.Equal(typeof(bool), remove.ReturnType);
        Assert.Contains(typeof(IDisposable), typeof(InteractionPrompt).GetInterfaces());

        AssertProperty(nameof(InteractionPrompt.Target), typeof(GameObject));
        AssertProperty(nameof(InteractionPrompt.Message), typeof(string));
        AssertProperty(nameof(InteractionPrompt.Input), typeof(InteractionPromptInput));
        AssertProperty(nameof(InteractionPrompt.State), typeof(InteractionPromptState));
        AssertProperty(nameof(InteractionPrompt.Range), typeof(float));
        AssertProperty(nameof(InteractionPrompt.Priority), typeof(int));
        AssertProperty(nameof(InteractionPrompt.IsAngleLimited), typeof(bool));
        AssertProperty(nameof(InteractionPrompt.AngleLimit), typeof(float));
        AssertProperty(nameof(InteractionPrompt.IsRemoved), typeof(bool));

        AssertEvent(nameof(InteractionPrompt.Hovered));
        AssertEvent(nameof(InteractionPrompt.InteractionStarted));
        AssertEvent(nameof(InteractionPrompt.InteractionEnded));
    }

    private static void AssertStaticFactory(Type declaringType, string methodName)
    {
        MethodInfo method = declaringType.GetMethod(methodName, new[] { typeof(GameObject) })!;
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(InteractionPromptBuilder), method.ReturnType);
        Assert.Equal("target", Assert.Single(method.GetParameters()).Name);
    }

    private static void AssertBuilderMethod(string methodName, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(InteractionPromptBuilder).GetMethod(methodName, parameterTypes)!;
        Assert.Equal(typeof(InteractionPromptBuilder), method.ReturnType);
    }

    private static void AssertHandleMethod(string methodName, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(InteractionPrompt).GetMethod(methodName, parameterTypes)!;
        Assert.Equal(typeof(InteractionPrompt), method.ReturnType);
    }

    private static void AssertProperty(string propertyName, Type propertyType)
    {
        PropertyInfo property = typeof(InteractionPrompt).GetProperty(propertyName)!;
        Assert.Equal(propertyType, property.PropertyType);
        Assert.NotNull(property.GetMethod);
    }

    private static void AssertEvent(string eventName)
    {
        EventInfo eventInfo = typeof(InteractionPrompt).GetEvent(eventName)!;
        Assert.Equal(typeof(Action), eventInfo.EventHandlerType);
    }
}

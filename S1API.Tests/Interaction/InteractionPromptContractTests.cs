using System;
using System.Reflection;
using S1API.Interaction;
using UnityEngine;

namespace S1API.Tests.Interaction;

public sealed class InteractionPromptContractTests
{
    [Fact]
    public void ExplicitBuilderConfigurationReplacesDefaults()
    {
        InteractionPromptBuilder builder = CreateManagedBuilderFixture();
        Action hovered = () => { };
        Action started = () => { };
        Action ended = () => { };

        InteractionPromptBuilder result = builder
            .WithMessage("Use")
            .WithInput(InteractionPromptInput.PrimaryClick)
            .WithState(InteractionPromptState.Label)
            .WithRange(2.5f)
            .WithPriority(8)
            .WithAngleLimit(60f)
            .OnHovered(hovered)
            .OnHovered(hovered)
            .OnInteractionStarted(started)
            .OnInteractionStarted(started)
            .OnInteractionEnded(ended)
            .OnInteractionEnded(ended);

        Assert.Same(builder, result);
        Assert.Equal("Use", builder.Message);
        Assert.Equal(InteractionPromptInput.PrimaryClick, builder.Input);
        Assert.Equal(InteractionPromptState.Label, builder.State);
        Assert.Equal(2.5f, builder.Range);
        Assert.Equal(8, builder.Priority);
        Assert.True(builder.LimitAngle);
        Assert.Equal(60f, builder.AngleLimit);
        Assert.Same(hovered, Assert.Single(builder.HoveredCallbacks));
        Assert.Same(started, Assert.Single(builder.InteractionStartedCallbacks));
        Assert.Same(ended, Assert.Single(builder.InteractionEndedCallbacks));
    }

    [Fact]
    public void FailedMessageValidationLeavesBuilderMutableForImmediateRetry()
    {
        InteractionPromptBuilder builder = CreateManagedBuilderFixture();

        Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Same(builder, builder.WithMessage("Retry"));
        Assert.Equal("Retry", builder.Message);
    }

    [Fact]
    public void BuiltBuilderReturnsCachedHandleAndRejectsFurtherMutation()
    {
        InteractionPromptBuilder builder = CreateManagedBuilderFixture();
        InteractionPrompt prompt = TestObjectFactory.CreateUninitialized<InteractionPrompt>();
        SetBuiltPrompt(builder, prompt);

        Assert.Same(prompt, builder.Build());
        Assert.Throws<InvalidOperationException>(() => builder.WithMessage("Changed"));
        Assert.Throws<InvalidOperationException>(() => builder.WithPriority(1));
        Assert.Throws<InvalidOperationException>(() => builder.OnHovered(() => { }));
    }

    [Fact]
    public void BuilderRejectsNullCallbacksAndUndefinedEnums()
    {
        InteractionPromptBuilder builder = CreateManagedBuilderFixture();

        Assert.Throws<ArgumentNullException>(() => builder.OnHovered(null!));
        Assert.Throws<ArgumentNullException>(() => builder.OnInteractionStarted(null!));
        Assert.Throws<ArgumentNullException>(() => builder.OnInteractionEnded(null!));
        Assert.Throws<ArgumentNullException>(
            () => builder.WithDisplayLocation((Transform)null!));
        Assert.Throws<ArgumentNullException>(
            () => builder.WithDisplayLocation((Collider)null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithInput((InteractionPromptInput)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithState((InteractionPromptState)99));
    }

    [Fact]
    public void PublicFactoriesRejectNullTargets()
    {
        Assert.Throws<ArgumentNullException>(() => InteractionPromptBuilder.Create(null!));
        Assert.Throws<ArgumentNullException>(() => InteractionPrompt.CreateBuilder(null!));
    }

    [Fact]
    public void NativeRangeIsBoundedByInteractionManagerCast()
    {
        Assert.Equal(4f, InteractionPromptContract.NativeMaxInteractionRange);
        Assert.Equal(90f, InteractionPromptContract.DefaultAngleLimit);
        Assert.Equal(0.1f, InteractionPromptContract.NormalizeRange(0.1f));
        Assert.Equal(4f, InteractionPromptContract.NormalizeRange(4f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(4.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void RangeRejectsUnsupportedValues(float range)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InteractionPromptContract.NormalizeRange(range));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(180.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void AngleLimitRejectsUnsupportedValues(float angleLimit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InteractionPromptContract.NormalizeAngleLimit(angleLimit));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MessageRejectsMissingText(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => InteractionPromptContract.NormalizeMessage(message!));
    }

    [Fact]
    public void PublicEnumsRemainStableAndNativeOrdered()
    {
        Assert.Equal(0, (int)InteractionPromptInput.Interact);
        Assert.Equal(1, (int)InteractionPromptInput.PrimaryClick);
        Assert.Equal(0, (int)InteractionPromptState.Default);
        Assert.Equal(1, (int)InteractionPromptState.Invalid);
        Assert.Equal(2, (int)InteractionPromptState.Disabled);
        Assert.Equal(3, (int)InteractionPromptState.Label);
    }

    [Fact]
    public void UndefinedEnumValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InteractionPromptContract.ValidateEnum(
                (InteractionPromptInput)99,
                "input"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InteractionPromptContract.ValidateEnum(
                (InteractionPromptState)99,
                "state"));
    }

    private static InteractionPromptBuilder CreateManagedBuilderFixture()
    {
        InteractionPromptBuilder builder =
            TestObjectFactory.CreateUninitialized<InteractionPromptBuilder>();
        SetPrivateField(builder, "_hoveredCallbacks", new List<Action>());
        SetPrivateField(builder, "_interactionStartedCallbacks", new List<Action>());
        SetPrivateField(builder, "_interactionEndedCallbacks", new List<Action>());
        return builder;
    }

    private static void SetBuiltPrompt(InteractionPromptBuilder builder, InteractionPrompt prompt)
    {
        SetPrivateField(builder, "_builtPrompt", prompt);
    }

    private static void SetPrivateField<TValue>(InteractionPromptBuilder builder, string name, TValue value)
    {
        FieldInfo field = typeof(InteractionPromptBuilder).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(builder, value);
    }
}

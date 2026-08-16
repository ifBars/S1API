using System;
using S1API.Interaction;

namespace S1API.Tests.Interaction;

public sealed class InteractionPromptContractTests
{
    [Fact]
    public void NativeRangeIsBoundedByInteractionManagerCast()
    {
        Assert.Equal(4f, InteractionPromptContract.NativeMaxInteractionRange);
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
}

using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCDialogueEventPolicyTests
{
    [Theory]
    [InlineData("ShopDialogue", "shopdialogue", true)]
    [InlineData("ShopDialogue", "OtherDialogue", false)]
    [InlineData("", "ShopDialogue", false)]
    [InlineData("ShopDialogue", "", false)]
    [InlineData(null, "ShopDialogue", false)]
    public void NamedChoiceContainerMatchingIsCaseInsensitiveAndSafe(
        string? candidateName,
        string? requestedName,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCDialoguePolicy.MatchesChoiceContainer(candidateName, requestedName));
    }

    [Fact]
    public void KeyedCallbacksAllowDuplicateRegistrationAndClearAllCallbacks()
    {
        var callbacks = new NPCDialogueCallbackRegistry();
        int invocationCount = 0;
        Action callback = () => invocationCount++;

        callbacks.Add("NODE", callback);
        callbacks.Add("node", callback);
        callbacks.Invoke("NoDe");

        Assert.Equal(2, invocationCount);

        callbacks.Clear();
        callbacks.Invoke("NODE");
        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public void OneCallbackFailureDoesNotSuppressLaterCallbacks()
    {
        var callbacks = new NPCDialogueCallbackRegistry();
        int invocationCount = 0;

        callbacks.Add("NODE", () => throw new InvalidOperationException("expected"));
        callbacks.Add("NODE", () => invocationCount++);
        callbacks.Invoke("NODE");

        Assert.Equal(1, invocationCount);
    }
}

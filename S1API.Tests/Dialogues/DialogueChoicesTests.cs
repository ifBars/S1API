using S1API.Dialogues;

namespace S1API.Tests.Dialogues;

public sealed class DialogueChoicesTests : IDisposable
{
    private readonly List<string> _log = new();

    public DialogueChoicesTests()
    {
        DialogueChoices.ResetForTesting();
        DialogueChoices.LogSinkForTesting = _log.Add;
    }

    public void Dispose()
    {
        DialogueChoices.ResetForTesting();
        DialogueChoices.LogSinkForTesting = null;
    }

    [Fact]
    public void ChoicesOfTwoModsOnOneMenuAreBothShownInOrder()
    {
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "mod_a", Text = "A" });
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "mod_b", Text = "B" });

        List<DialogueChoiceSpec> shown = DialogueChoices.ShownChoices("ray_hoffman", "Realtor_SellProperty", "ENTRY");

        Assert.Equal(new[] { "mod_a", "mod_b" }, shown.Select(s => s.Label));
    }

    [Fact]
    public void ChoiceMatchesOnlyItsNpcNodeAndConversation()
    {
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec
        {
            Label = "agent_only",
            ConversationName = "Realtor_SellProperty"
        });

        Assert.Single(DialogueChoices.ShownChoices("ray_hoffman", "Realtor_SellProperty", "ENTRY"));
        Assert.Empty(DialogueChoices.ShownChoices("ray_hoffman", "Generic", "ENTRY"));
        Assert.Empty(DialogueChoices.ShownChoices("ray_hoffman", "Realtor_SellProperty", "CONFIRM"));
        Assert.Empty(DialogueChoices.ShownChoices("jeremy_wilkinson", "Realtor_SellProperty", "ENTRY"));
    }

    [Fact]
    public void IsShownIsEvaluatedEachTimeAndAThrowHidesOnlyThatChoice()
    {
        bool open = false;
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "toggle", IsShown = () => open });
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "broken", IsShown = () => throw new InvalidOperationException() });
        DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "always" });

        Assert.Equal(new[] { "always" }, DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY").Select(s => s.Label));
        open = true;
        Assert.Equal(new[] { "toggle", "always" }, DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY").Select(s => s.Label));
        Assert.Contains(_log, line => line.StartsWith("error: S1API.Tests: IsShown of choice 'broken' threw"));
    }

    [Fact]
    public void DisposeRemovesTheChoice()
    {
        IDisposable handle = DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = "temp" });

        handle.Dispose();
        handle.Dispose();

        Assert.Empty(DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY"));
    }

    [Fact]
    public void HideFollowsItsConditionAndStopsAfterDispose()
    {
        bool hidden = true;
        IDisposable handle = DialogueChoices.Hide("ray_hoffman", "storageunit", () => hidden);

        Assert.True(DialogueChoices.IsHidden("ray_hoffman", "storageunit"));
        Assert.False(DialogueChoices.IsHidden("ray_hoffman", "bungalow"));
        Assert.False(DialogueChoices.IsHidden("jeremy_wilkinson", "storageunit"));
        hidden = false;
        Assert.False(DialogueChoices.IsHidden("ray_hoffman", "storageunit"));
        hidden = true;
        handle.Dispose();
        Assert.False(DialogueChoices.IsHidden("ray_hoffman", "storageunit"));
    }

    [Theory]
    [InlineData(null, false, 3)]
    [InlineData(null, true, 2)]
    [InlineData("buy_business", false, 2)]
    [InlineData("buy_business", true, 2)]
    [InlineData("missing", true, 2)]
    [InlineData("missing", false, 3)]
    public void InsertIndexUsesAnchorThenBeforeLastThenEnd(string? after, bool beforeLast, int expected)
    {
        var labels = new List<string?> { "buy_property", "buy_business", "" };
        var spec = new DialogueChoiceSpec { Label = "new", InsertAfterLabel = after, InsertBeforeLastChoice = beforeLast };

        Assert.Equal(expected, DialogueChoices.InsertIndex(labels, spec));
    }

    [Fact]
    public void InsertBeforeLastOnAnEmptyMenuAppends()
    {
        var spec = new DialogueChoiceSpec { Label = "new", InsertBeforeLastChoice = true };

        Assert.Equal(0, DialogueChoices.InsertIndex(new List<string?>(), spec));
    }

    [Fact]
    public void CheckEnabledReturnsReasonAndTreatsAThrowAsEnabled()
    {
        var disabled = new DialogueChoiceSpec { Label = "d", IsEnabled = () => false, DisabledReason = "Need $200" };
        var broken = new DialogueChoiceSpec { Label = "b", IsEnabled = () => throw new InvalidOperationException() };

        Assert.False(DialogueChoices.CheckEnabled(disabled, out string reason));
        Assert.Equal("Need $200", reason);
        Assert.True(DialogueChoices.CheckEnabled(broken, out _));
        Assert.True(DialogueChoices.CheckEnabled(new DialogueChoiceSpec { Label = "e" }, out _));
    }

    [Fact]
    public void FindReturnsTheSpecOnlyInItsMenuAndRunChosenSwallowsAThrow()
    {
        int runs = 0;
        var spec = new DialogueChoiceSpec { Label = "pick", OnChosen = () => runs++ };
        var broken = new DialogueChoiceSpec { Label = "boom", OnChosen = () => throw new InvalidOperationException() };
        DialogueChoices.Add("ray_hoffman", "ENTRY", spec);
        DialogueChoices.Add("ray_hoffman", "ENTRY", broken);

        Assert.Same(spec, DialogueChoices.Find("ray_hoffman", null, "ENTRY", "pick"));
        Assert.Null(DialogueChoices.Find("ray_hoffman", null, "CONFIRM", "pick"));
        DialogueChoices.RunChosen(spec);
        DialogueChoices.RunChosen(broken);
        Assert.Equal(1, runs);
    }

    [Fact]
    public void AddRejectsMissingArguments()
    {
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("", "ENTRY", new DialogueChoiceSpec { Label = "x" }));
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("ray_hoffman", "", new DialogueChoiceSpec { Label = "x" }));
        Assert.Throws<ArgumentNullException>(() => DialogueChoices.Add("ray_hoffman", "ENTRY", null!));
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec()));
    }
}

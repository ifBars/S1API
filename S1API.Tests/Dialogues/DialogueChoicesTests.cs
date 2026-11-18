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
    [InlineData("buy_property", true, 1)]
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
    public void RunChosenRunsTheCallbackAndSwallowsAThrow()
    {
        int runs = 0;
        var spec = new DialogueChoiceSpec { Label = "pick", OnChosen = () => runs++ };
        var broken = new DialogueChoiceSpec { Label = "boom", OnChosen = () => throw new InvalidOperationException() };
        DialogueChoices.Add("ray_hoffman", "ENTRY", spec);
        DialogueChoices.Add("ray_hoffman", "ENTRY", broken);

        DialogueChoices.RunChosen(spec);
        DialogueChoices.RunChosen(broken);

        Assert.Equal(1, runs);
        Assert.Contains(_log, line => line.StartsWith("error: S1API.Tests: OnChosen of choice 'boom' threw"));
    }

    [Fact]
    public void OwnerIsStillNamedWhenTheCallbackDisposesItsOwnRegistrationAndThrows()
    {
        IDisposable? handle = null;
        handle = DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec
        {
            Label = "once",
            OnChosen = () =>
            {
                handle!.Dispose();
                throw new InvalidOperationException();
            }
        });
        DialogueChoiceSpec spec = DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY").Single();

        DialogueChoices.RunChosen(spec);

        Assert.Contains(_log, line => line.StartsWith("error: S1API.Tests: OnChosen of choice 'once' threw"));
        Assert.DoesNotContain(_log, line => line.Contains("unknown mod"));
    }

    [Fact]
    public void ARowStaysBoundToItsRegistrationSoAVanillaRowWithTheSameLabelIsNeverTouched()
    {
        var disabled = new DialogueChoiceSpec { Label = "buy_property", IsEnabled = () => false };
        DialogueChoices.Add("ray_hoffman", "ENTRY", disabled);
        object handler = new object();

        // The vanilla row owns the label, so the patch inserts and binds nothing for it.
        Assert.Empty(DialogueChoices.FindBoundByLabel(handler, "buy_property"));
        Assert.Null(DialogueChoices.FindBoundByRow(handler, "vanilla-row"));

        DialogueChoices.Bind(handler, "mod-row", "other_label", disabled);
        Assert.Same(disabled, DialogueChoices.FindBoundByRow(handler, "mod-row"));
        Assert.Empty(DialogueChoices.FindBoundByLabel(handler, "buy_property"));
        Assert.Null(DialogueChoices.FindBoundByRow(handler, "vanilla-row"));
    }

    [Fact]
    public void TwoRegistrationsWithOneLabelStayBoundToTheirOwnRows()
    {
        int runsA = 0, runsB = 0;
        var a = new DialogueChoiceSpec { Label = "shared", OnChosen = () => runsA++ };
        var b = new DialogueChoiceSpec { Label = "shared", OnChosen = () => runsB++ };
        DialogueChoices.Add("ray_hoffman", "ENTRY", a);
        DialogueChoices.Add("ray_hoffman", "ENTRY", b);
        object handler = new object();
        DialogueChoices.Bind(handler, "row-a", "shared", a);
        DialogueChoices.Bind(handler, "row-b", "shared", b);

        DialogueChoices.RunChosen(DialogueChoices.FindBoundByRow(handler, "row-b")!);

        Assert.Equal(0, runsA);
        Assert.Equal(1, runsB);
        Assert.Equal(new[] { a, b }, DialogueChoices.FindBoundByLabel(handler, "shared"));
    }

    [Fact]
    public void BindingsEndWithTheMenuRebuildOrTheRegistration()
    {
        var spec = new DialogueChoiceSpec { Label = "temp" };
        IDisposable handle = DialogueChoices.Add("ray_hoffman", "ENTRY", spec);
        object handler = new object();
        DialogueChoices.Bind(handler, "row", "temp", spec);

        DialogueChoices.ClearBindings(new object());
        Assert.Same(spec, DialogueChoices.FindBoundByRow(handler, "row"));
        DialogueChoices.ClearBindings(handler);
        Assert.Null(DialogueChoices.FindBoundByRow(handler, "row"));

        DialogueChoices.Bind(handler, "row", "temp", spec);
        handle.Dispose();
        Assert.Null(DialogueChoices.FindBoundByRow(handler, "row"));
    }

    [Fact]
    public void ALabelThatTurnsBlankAfterAddIsSkippedAndWarnedOnce()
    {
        var spec = new DialogueChoiceSpec { Label = "fine" };
        DialogueChoices.Add("ray_hoffman", "ENTRY", spec);
        spec.Label = "  ";

        Assert.Empty(DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY"));
        Assert.Empty(DialogueChoices.ShownChoices("ray_hoffman", null, "ENTRY"));
        Assert.Single(_log, line => line.StartsWith("warn: ") && line.Contains("blank label"));
    }

    [Fact]
    public void AddRejectsMissingArguments()
    {
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("", "ENTRY", new DialogueChoiceSpec { Label = "x" }));
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("ray_hoffman", "", new DialogueChoiceSpec { Label = "x" }));
        Assert.Throws<ArgumentNullException>(() => DialogueChoices.Add("ray_hoffman", "ENTRY", null!));
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec()));
        Assert.Throws<ArgumentException>(() => DialogueChoices.Add("ray_hoffman", "ENTRY", new DialogueChoiceSpec { Label = " \t" }));
    }
}

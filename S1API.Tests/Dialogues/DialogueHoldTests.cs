using System.Reflection;
using HarmonyLib;
using S1API.Dialogues;
using S1API.Internal.Dialogues;
using S1API.Internal.Patches;

#if IL2CPPMELON
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1UI = Il2CppScheduleOne.UI;
#elif MONOMELON
using S1Dialogue = ScheduleOne.Dialogue;
using S1UI = ScheduleOne.UI;
#endif

namespace S1API.Tests.Dialogues;

[Collection("DialogueHolds")]
public sealed class DialogueHoldTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();
    private readonly object _ray = new();
    private readonly object _other = new();

    public DialogueHoldTests()
    {
        DialogueHolds.ResetForTesting();
        DialogueHolds.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose() => DialogueHolds.ResetForTesting();

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetMethod("Update", flags, null, Type.EmptyTypes, null));
#if MONOMELON
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetField("_continuePressed", flags));
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetField("continueChoice", flags));
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetField("currentHandler", flags));
#else
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetProperty("_continuePressed"));
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetProperty("continueChoice"));
        Assert.NotNull(typeof(S1UI.DialogueCanvas).GetProperty("currentHandler"));
#endif
    }

    [Fact]
    public void PatchMethodsCarryHarmonyAttributes()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        // No prefix on ContinueSubmitted: the canvas coroutine has already hidden the prompt when it calls it.
        Assert.Null(typeof(DialogueFlowPatches).GetMethod("ContinueSubmittedPrefix", flags));
        Assert.NotNull(typeof(DialogueFlowPatches).GetMethod("CanvasUpdatePostfix", flags)!.GetCustomAttribute<HarmonyPostfix>());
    }

    [Fact]
    public void HoldBlocksOnlyItsOwnerWhileTrue()
    {
        bool hold = true;
        DialogueHolds.Add(_ray, "ray", () => hold);

        Assert.True(DialogueHolds.IsHeld(o => o == _ray));
        Assert.False(DialogueHolds.IsHeld(o => o == _other));
        hold = false;
        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        Assert.Contains(_log, l => l.Message == "Continue on ray is held; the dialogue stays open.");
    }

    [Fact]
    public void BlockAndReleaseAreLoggedOncePerHoldPhase()
    {
        bool hold = true;
        DialogueHolds.Add(_ray, "ray", () => hold);

        Assert.True(DialogueHolds.IsHeld(o => o == _ray));
        Assert.True(DialogueHolds.IsHeld(o => o == _ray));
        Assert.True(DialogueHolds.IsHeld(o => o == _ray));
        hold = false;
        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        hold = true;
        Assert.True(DialogueHolds.IsHeld(o => o == _ray));

        Assert.Equal(2, _log.Count(l => l.Message == "Continue on ray is held; the dialogue stays open."));
        Assert.Single(_log, l => l.Message == "Continue hold on ray released; the press goes through.");
    }

    [Fact]
    public void SceneChangeRemovesHoldsAndPendingPress()
    {
        DialogueHolds.Add(_ray, "ray", () => true);
        DialogueHolds.Add(_other, "other", () => true);
        DialogueHolds.PendingContinue = true;

        DialogueHolds.ClearForSceneChange();

        Assert.Equal(0, DialogueHolds.Count);
        Assert.False(DialogueHolds.PendingContinue);
        Assert.Single(_log, l => l.Message == "Continue holds removed on the scene change: 2.");
    }

    [Fact]
    public void PendingPressIsDroppedWithALogLine()
    {
        DialogueHolds.PendingContinue = true;

        DialogueHolds.DropPendingContinue();
        DialogueHolds.DropPendingContinue();

        Assert.False(DialogueHolds.TakePendingContinue());
        Assert.Single(_log, l => l.Message == "Continue from code dropped: the dialogue closed before the press was applied.");
        Assert.DoesNotContain(_log, l => l.Message == "Continue pressed from code.");
    }

    [Fact]
    public void DisposeRemovesTheHold()
    {
        var handle = DialogueHolds.Add(_ray, "ray", () => true);
        handle.Dispose();
        handle.Dispose();

        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        Assert.Equal(0, DialogueHolds.Count);
        Assert.Single(_log, l => l.Message == "Continue hold on ray removed.");
    }

    [Fact]
    public void ThrowingHoldIsLoggedOnceAndReleased()
    {
        DialogueHolds.Add(_ray, "ray", () => throw new InvalidOperationException("boom"));

        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        Assert.False(DialogueHolds.IsHeld(o => o == _ray));
        Assert.Single(_log, l => l.Level == "error" && l.Message.Contains("treated as released"));
    }

    [Fact]
    public void PendingContinueIsTakenOnce()
    {
        DialogueHolds.PendingContinue = true;

        Assert.True(DialogueHolds.TakePendingContinue());
        Assert.False(DialogueHolds.TakePendingContinue());
        Assert.Single(_log, l => l.Message == "Continue pressed from code.");
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => DialogueHolds.Add(_ray, "ray", null!));
        Assert.Throws<ArgumentNullException>(() => DialogueHolds.Add(null!, "ray", () => true));
    }

    [Fact]
    public void PublicSurfaceExists()
    {
        Assert.NotNull(typeof(DialogueFlow).GetMethod("TryContinue", Type.EmptyTypes));
        Assert.NotNull(typeof(DialogueFlow).GetProperty("IsOpen"));
        Assert.NotNull(typeof(global::S1API.Entities.NPCDialogue).GetMethod("HoldContinue", new[] { typeof(Func<bool>) }));
    }
}

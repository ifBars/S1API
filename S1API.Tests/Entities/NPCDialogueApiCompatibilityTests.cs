using System;
using System.Linq;
using System.Reflection;
using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCDialogueApiCompatibilityTests
{
    [Fact]
    public void DialogueCompletionCallbackIsAdditiveAndFluent()
    {
        MethodInfo? method = typeof(NPCDialogue).GetMethod(
            nameof(NPCDialogue.OnDialogueEnded),
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Action) },
            null);

        Assert.NotNull(method);
        Assert.Equal(typeof(NPCDialogue), method!.ReturnType);
        Assert.Equal("callback", method.GetParameters()[0].Name);
    }

    [Fact]
    public void NamedChoiceStateSetterUsesOnlyManagedArguments()
    {
        MethodInfo? method = typeof(NPCDialogue).GetMethod(
            nameof(NPCDialogue.SetChoiceEnabled),
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(bool) },
            null);

        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        Assert.Equal(
            new[] { "dialogueContainerName", "enabled" },
            method.GetParameters().Select(parameter => parameter.Name).ToArray());
        Assert.DoesNotContain(
            method.GetParameters(),
            parameter => parameter.ParameterType.FullName?.Contains("ScheduleOne", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ExistingDialogueCallbackMembersRemainAvailable()
    {
        Assert.NotNull(typeof(NPCDialogue).GetMethod(nameof(NPCDialogue.OnChoiceSelected)));
        Assert.NotNull(typeof(NPCDialogue).GetMethod(nameof(NPCDialogue.OnNodeDisplayed)));
        Assert.NotNull(typeof(NPCDialogue).GetMethod(nameof(NPCDialogue.OnConversationStart)));
        Assert.NotNull(typeof(NPCDialogue).GetMethod(nameof(NPCDialogue.ClearCallbacks)));
    }
}

internal static class NPCDialogueApiCompileFixture
{
    internal static NPCDialogue RegisterCallbacks(NPCDialogue dialogue, Action callback)
    {
        return dialogue
            .OnChoiceSelected("CHOICE", callback)
            .OnNodeDisplayed("NODE", callback)
            .OnConversationStart(callback)
            .OnDialogueEnded(callback);
    }

    internal static bool SetChoiceState(NPCDialogue dialogue, bool enabled) =>
        dialogue.SetChoiceEnabled("ShopDialogue", enabled);
}

using System;
using S1API.Interaction;
using UnityEngine;

namespace S1API.Tests.Interaction;

internal static class InteractionPromptApiCompileFixture
{
    internal static InteractionPrompt Configure(
        GameObject target,
        Collider displayCollider,
        Transform displayPoint,
        Action onHovered,
        Action onStarted,
        Action onEnded)
    {
        return InteractionPrompt
            .CreateBuilder(target)
            .WithMessage("Use machine")
            .WithInput(InteractionPromptInput.Interact)
            .WithState(InteractionPromptState.Default)
            .WithRange(3f)
            .WithPriority(5)
            .WithAngleLimit(75f)
            .WithoutAngleLimit()
            .WithDisplayLocation(displayPoint)
            .WithDisplayLocation(displayCollider)
            .OnHovered(onHovered)
            .OnInteractionStarted(onStarted)
            .OnInteractionEnded(onEnded)
            .Build();
    }

    internal static void ConfigureRuntime(InteractionPrompt prompt, Collider displayCollider, Transform displayPoint)
    {
        prompt
            .SetMessage("Stop machine")
            .SetInput(InteractionPromptInput.PrimaryClick)
            .SetState(InteractionPromptState.Invalid)
            .SetRange(2f)
            .SetPriority(10)
            .SetAngleLimit(45f)
            .WithoutAngleLimit()
            .SetDisplayLocation(displayPoint)
            .SetDisplayLocation(displayCollider)
            .ClearDisplayLocation();

        _ = prompt.Target;
        _ = prompt.Message;
        _ = prompt.Input;
        _ = prompt.State;
        _ = prompt.Range;
        _ = prompt.Priority;
        _ = prompt.IsAngleLimited;
        _ = prompt.AngleLimit;
        _ = prompt.IsRemoved;
        prompt.Remove();
        prompt.Dispose();
    }
}

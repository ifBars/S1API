# Native Interaction Prompts

`InteractionPrompt` attaches the game's native interaction prompt to a mod-owned `GameObject`.
The game remains responsible for raycasts, input-device glyphs, prompt rendering, overlap priority,
and the interaction lifecycle.

```csharp
private InteractionPrompt? _prompt;

void ConfigureMachine(GameObject machine, Collider interactionCollider)
{
    _prompt = InteractionPrompt
        .CreateBuilder(machine)
        .WithMessage("Start mixer")
        .WithDisplayLocation(interactionCollider)
        .WithRange(3f)
        .WithPriority(5)
        .OnInteractionStarted(StartMixer)
        .OnInteractionEnded(StopHoldingMixer)
        .Build();
}
```

The target hierarchy must contain a collider on a layer included by the game's interaction search
mask. S1API does not create a collider or change the target's layer. A collider supplied through
`WithDisplayLocation(Collider)` controls only where the prompt is rendered; it is not a replacement
for the target's interaction collider.

## Prompt configuration

The builder supports the native input actions `Interact` and `PrimaryClick`, the native states
`Default`, `Invalid`, `Disabled`, and `Label`, a maximum range of four metres, overlap priority,
and an optional horizontal angle limit. The four-metre limit comes from the game's interaction
raycast, so a larger per-component value would not make the prompt reachable.

If neither display location overload is used, the prompt renders at the target transform. A
transform uses its position directly. A collider uses the closest point to the player, which is
useful for large machines and furniture.

## Runtime updates and callbacks

The returned handle can update the message, input, state, range, priority, angle limit, and display
location:

```csharp
_prompt
    ?.SetState(canUse
        ? InteractionPromptState.Default
        : InteractionPromptState.Invalid)
    .SetMessage(canUse ? "Start mixer" : "Mixer is busy");
```

`OnHovered` follows the native event and may run every frame while the prompt is selected.
`OnInteractionStarted` and `OnInteractionEnded` follow the game's input-hold lifecycle. These
callbacks do not provide multiplayer authorization or synchronization. The mod must validate the
request and use its own network policy before changing shared state.

Call `Remove()` or `Dispose()` when the mod-owned object is retired. Removing the prompt destroys
only the S1API-owned native component and leaves the target, colliders, and other components intact.

The builder rejects targets that already contain an `InteractableObject`; configure an existing
native interaction component directly when a prefab already owns one or use a separate child
target for an additional prompt.

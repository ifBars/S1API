# Dialogue Injection

`DialogueInjection` lets you attach an extra choice to an existing vanilla or custom NPC dialogue flow without replacing the whole container.

This is useful when you want to extend an NPC's normal conversation with one additional branch, such as a service unlock, a follow-up action, or a context-specific option that should only appear for a specific character.

## Overview

The injection flow has two parts:

1. Create a `DialogueInjection` that describes where the new choice should be inserted
2. Register it with `DialogueInjector.Register(...)`

S1API then waits until the matching NPC is available in the world, finds the requested dialogue container and source node, appends the new choice, creates the node link, and wires up your confirmation callback.

```csharp
using S1API.Dialogues;

DialogueInjector.Register(new DialogueInjection(
    npc: "Philip",
    container: "CasinoDialogue",
    from: "ROOT_NODE_GUID",
    to: "BANKING_NODE_GUID",
    label: "ASK_ABOUT_PAYOUTS",
    text: "Quick question about payouts.",
    onConfirmed: () =>
    {
        // Handle the selected option
    }));
```

## When to Use It

Use `DialogueInjection` when you want to:

- Add one or more new choices to an existing NPC dialogue tree
- Reuse an existing container instead of building a full replacement
- Hook into a known dialogue node and branch into your own follow-up node
- Attach lightweight interaction logic to a dialogue choice callback

If you need to build a full custom dialogue flow from scratch, start with **[Dialogue System](dialogue-system.md)** instead.

## Constructor Options

`DialogueInjection` supports two ways to target an NPC.

### By NPC ID

Use the string overload when you know the NPC's ID.

```csharp
var injection = new DialogueInjection(
    npc: "Philip",
    container: "CasinoDialogue",
    from: "ROOT_NODE_GUID",
    to: "BANKING_NODE_GUID",
    label: "ASK_ABOUT_PAYOUTS",
    text: "Quick question about payouts.",
    onConfirmed: OnAskAboutPayouts);
```

Internally this maps to `x => x.ID.Equals(npc)`.

### By Predicate

Use the predicate overload when you need more control.

```csharp
using S1API.Entities;

var injection = new DialogueInjection(
    appliesToNpc: npc => npc.ID == "Philip" && npc.FullName.Contains("Philip"),
    container: "CasinoDialogue",
    from: "ROOT_NODE_GUID",
    to: "BANKING_NODE_GUID",
    label: "ASK_ABOUT_PAYOUTS",
    text: "Quick question about payouts.",
    onConfirmed: OnAskAboutPayouts);
```

This is helpful when multiple NPC variants share related data or when your targeting rule depends on runtime state.

## Field Reference

Each `DialogueInjection` instance needs the following values:

- `AppliesTo`: Predicate used to decide whether a world NPC should receive the injection
- `ContainerName`: Name of the target dialogue container
- `FromNodeGuid`: GUID of the node that should receive the new choice
- `ToNodeGuid`: GUID of the node the new choice should lead to
- `ChoiceLabel`: Internal identifier used for callback registration
- `ChoiceText`: Text shown to the player in the dialogue UI
- `OnConfirmed`: Action invoked when the player selects the injected choice

## How Registration Works

When you call `DialogueInjector.Register(...)`:

1. The injection is queued
2. S1API starts a lightweight wait loop if one is not already running
3. The loop checks loaded NPCs until one matches `AppliesTo`
4. The injector resolves the requested container by name
5. The injector finds the source node by `FromNodeGuid`
6. A new choice and node link are added to that dialogue container
7. `DialogueChoiceListener` binds your `OnConfirmed` callback to `ChoiceLabel`

This means you can usually register injections during your mod setup without waiting for the NPC to already be spawned.

## Example

The example below adds a new question to an existing NPC dialogue and sends the player into a custom follow-up node when selected.

```csharp
using S1API.Dialogues;

public static class PayoutDialogueFeature
{
    public static void Register()
    {
        DialogueInjector.Register(new DialogueInjection(
            npc: "Philip",
            container: "CasinoDialogue",
            from: "INTRO_NODE_GUID",
            to: "PAYOUT_INFO_NODE_GUID",
            label: "ASK_PAYOUT_QUESTION",
            text: "Quick question about payouts.",
            onConfirmed: OnPayoutQuestionSelected));
    }

    private static void OnPayoutQuestionSelected()
    {
        // Put your feature logic here
    }
}
```

In practice, the target node identified by `ToNodeGuid` must already exist in the container you are linking into.

## Requirements and Limitations

- `ContainerName` must match a real container name exactly
- `FromNodeGuid` must point to an existing node in that container
- `ToNodeGuid` must point to an existing destination node
- `ChoiceLabel` should be unique enough for your mod's callback usage
- If the NPC, container, or node cannot be found, the injection is skipped silently

Because this API works against existing dialogue assets, you should verify container names and GUIDs carefully before shipping.

## Best Practices

- Prefer stable NPC IDs over loose matching when possible
- Keep `ChoiceLabel` descriptive and mod-specific to avoid collisions
- Use player-facing `ChoiceText` that fits the NPC's existing conversation tone
- Keep `OnConfirmed` fast and push larger flows into your own dialogue nodes or systems
- Test the target dialogue path in-game after save loads and scene changes

## Troubleshooting

### The choice never appears

Check these first:

- The NPC ID or predicate actually matches a loaded `S1API.Entities.NPC`
- The container name is correct
- The source node GUID exists in that container
- The target node GUID exists and is reachable

### The callback does not run

Make sure:

- `ChoiceLabel` is unique for the injected choice
- `OnConfirmed` does not throw immediately
- Another mod is not reusing the same choice label in the same dialogue flow

## Next Steps

- Read **[Dialogue System](dialogue-system.md)** for building full containers and custom nodes
- Use `DialogueInjection` for small extensions and `Dialogue.BuildAndRegisterContainer(...)` for larger custom flows

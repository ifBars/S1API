---
name: schedule-one-custom-npcs
description: Focused Schedule One S1API custom NPC creation skill. Use when creating, editing, debugging, or reviewing custom NPC classes for Schedule 1 mods, including physical or non-physical NPCs, ConfigurePrefab builder setup, appearance, schedules, dialogue, customer behavior, dealer behavior, runtime lifecycle hooks, save/load callback restoration, and AvatarFramework-backed appearance ranges and asset paths.
---

# Schedule One Custom NPCs

Use this skill to build custom NPCs with `S1API` using the documented two-phase model and the underlying `AvatarFramework` constraints.

## Workflow

Follow this order:

1. Classify the NPC as `physical`, `non-physical`, `customer`, `dealer`, or a mixed interaction/UI contact.
2. Put persistent prefab-time data in `ConfigurePrefab(...)`.
3. Put runtime behavior in `OnCreated()` and add `OnDestroyed()`, `OnLoaded()`, or `OnResponseLoaded(...)` when needed.
4. Prefer S1API builder and wrapper APIs over direct low-level object manipulation.
5. Return minimal, reviewable changes plus explicit manual verification steps.

## Core Model

Keep these responsibilities separate:

- `ConfigurePrefab(...)`: identity, icon, spawn position, relationship defaults, customer defaults, dealer defaults, inventory defaults, schedule, and required `Ensure*` components.
- `OnCreated()`: `base.OnCreated()`, `Appearance.Build()`, `Schedule.Enable()`, `Schedule.InitializeActions()` when needed, dialogue wiring, event subscriptions, text messages, and runtime state.

Do not move persistent customer, dealer, relationship, or schedule defaults into runtime code.

## Decision Rules

### Physical vs non-physical

- `IsPhysical => true`: world entity, avatar, spawn point, schedule, direct interaction.
- `IsPhysical => false`: messaging/contact-focused NPC, usually no spawn point and no schedule.

### Customer vs dealer

- Customer NPCs need `EnsureCustomer()` before `WithCustomerDefaults(...)`.
- Customer schedules usually need `plan.EnsureDealSignal()`.
- Dealer NPCs need `public override bool IsDealer => true;` plus `EnsureDealer()` and `WithDealerDefaults(...)`.
- Dealer schedules need `plan.EnsureDealSignal()` to function correctly, and may use `plan.HandleDeal(...)` when that better fits the role.

## Hard Rules

- Prefer the parameterless NPC constructor for new code.
- Never manually instantiate custom NPCs with `new`; let S1API own instancing.
- Treat `WithIdentity(id, ...)` as stable save data. Changing the `id` effectively creates a different NPC.
- Configure customer, dealer, relationship, and schedule defaults only in `ConfigurePrefab(...)`.
- Always call `Appearance.Build()` after runtime appearance changes.
- For physical NPCs, call `Schedule.Enable()` in `OnCreated()`; add `Schedule.InitializeActions()` when actions require it.
- Unsubscribe from events in `OnDestroyed()`.
- Reattach saved text-message callbacks in `OnResponseLoaded(...)` when messages must keep working after load.
- Restore load-sensitive runtime state in `OnLoaded()`.
- Call `ClearConversationCategories()` for contacts that should not show the default Customer/Supplier/Dealer badge.
- Prefer schedule wrapper methods like `WalkTo(...)`, `StayInBuilding(...)`, `UseVendingMachine(...)`, `LocationDialogue(...)`, and `HandleDeal(...)`; use `plan.Add(new ...Spec())` only for advanced cases.
- Before using `LocationBased(...).OnArriveSmokeBreak()`, `OnArriveGraffiti()`, `OnArriveDrinking()`, or `OnArriveHoldItem()`, add the corresponding prefab-time `Ensure*` component.
- Do not call `Dialogue.StopOverride()` from `OnNodeDisplayed(...)`; only call it from safe callback points such as `OnChoiceSelected(...)`.

## Minimal Skeletons

### Physical NPC

```csharp
using S1API.Entities;
using UnityEngine;

public sealed class MyCustomNpc : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var spawnPos = new Vector3(0f, 0f, 0f);

        builder.WithIdentity("my_custom_npc", "Alex", "Example")
            .WithSpawnPosition(spawnPos)
            .WithRelationshipDefaults(r =>
            {
                r.WithDelta(1.0f)
                    .SetUnlocked(true)
                    .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
            })
            .WithSchedule(plan =>
            {
                plan.WalkTo(spawnPos, 900);
            });
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Appearance.Build();
        Schedule.Enable();
    }
}
```

### Non-physical contact NPC

```csharp
using S1API.Entities;

public sealed class ContactNpc : NPC
{
    public override bool IsPhysical => false;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("contact_npc", "Unknown", "Contact")
            .WithIcon(null);
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        ClearConversationCategories();
        SendTextMessage("Hello from the contact.");
    }
}
```

## Appearance Rules

Read `references/s1api-custom-npc-reference.md` for the detailed appearance and AvatarFramework notes. The most important constraints are:

- `Gender`: treat as normalized `0.0f` to `1.0f`; `Avatar.IsMale()` uses `< 0.5f`.
- `Weight`: treat as normalized `0.0f` to `1.0f`; AvatarFramework applies it as a blendshape percentage.
- `Height`: AvatarFramework does not clamp it, but S1API defaults and random generation center around `0.98f` to `1.0f`; use `0.8f` to `1.2f` as the safe practical range unless you intentionally want exaggerated scaling.
- `PupilDilation`: use `0.0f` to `1.0f`.
- `EyeLidRestingState` values: each lid value should stay in `0.0f` to `1.0f`.
- `EyebrowRestingHeight`: runtime code clamps this to `-1.1f` through `1.5f`.
- Practical layer limits: keep face layers to 6, body layers to 6, and accessories to 9.
- Asset paths must match `Resources.Load(...)` style paths, such as `Avatar/Hair/Spiky/Spiky` or `Avatar/Layers/Top/T-Shirt`, with no file extension.

## Reference Files

- Read `references/s1api-custom-npc-reference.md` for API guidance, runtime lifecycle behavior, schedule rules, and AvatarFramework-backed appearance constraints.
- Read `references/example-project-patterns.md` for reusable implementation patterns without depending on any local sample repository.

## Output Expectations

When producing code or guidance, include:

- The NPC type and why it fits.
- What belongs in `ConfigurePrefab(...)` versus `OnCreated()`.
- Which files need to change.
- Manual checks for spawn, mugshot/icon, interaction, schedule execution, customer/dealer behavior, save/load restoration, and any message-response callbacks.

## Common Pitfalls

- Setting appearance defaults but forgetting `Appearance.Build()`.
- Calling `WithCustomerDefaults(...)` without `EnsureCustomer()`.
- Calling `WithDealerDefaults(...)` without `IsDealer => true`.
- Omitting `EnsureDealSignal()` for customer or dealer schedules that need deals/contracts.
- Using advanced location-based actions without the matching `Ensure*` call.
- Using `plan.Add(new ...Spec())` for simple cases where a wrapper method is clearer.
- Modifying persistent defaults in `OnCreated()` instead of `ConfigurePrefab(...)`.
- Forgetting to restore message response callbacks in `OnResponseLoaded(...)`.
- Changing the NPC `id` without realizing it changes save identity.

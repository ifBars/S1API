# Implementation Patterns

## Purpose

This file captures reusable S1API custom NPC implementation patterns without assuming any local sample repository exists.

## Pattern 1: Physical Customer NPC

Use when the NPC is visible in the world, directly interactable, and participates in the customer economy.

Recommended structure:

```csharp
builder.WithIdentity(...)
    .WithAppearanceDefaults(...)
    .WithSpawnPosition(...)
    .EnsureCustomer()
    .WithCustomerDefaults(...)
    .WithRelationshipDefaults(...)
    .WithSchedule(plan =>
    {
        plan.EnsureDealSignal()
            .WalkTo(...)
            .StayInBuilding(...);
    });
```

Typical runtime work:

- `base.OnCreated()`
- `Appearance.Build()`
- `Dialogue.BuildAndRegisterContainer(...)`
- `Dialogue.OnChoiceSelected(...)`
- `Schedule.Enable()`

Extra checks:

- Confirm `EnsureCustomer()` exists before `WithCustomerDefaults(...)`.
- Confirm the schedule includes `EnsureDealSignal()` when the customer should actively deal.
- Keep spending, standards, and relationship requirements internally consistent.

## Pattern 2: Event-wired Customer NPC

Use when customer events drive other behavior such as messages, relationship gains, recommendations, or rewards.

Recommended structure:

```csharp
private Action _customerDealCompletedHandler;

protected override void OnCreated()
{
    base.OnCreated();

    _customerDealCompletedHandler ??= HandleDealCompleted;
    Customer.OnDealCompleted -= _customerDealCompletedHandler;
    Customer.OnDealCompleted += _customerDealCompletedHandler;
}

protected override void OnDestroyed()
{
    base.OnDestroyed();

    if (Customer != null && _customerDealCompletedHandler != null)
    {
        Customer.OnDealCompleted -= _customerDealCompletedHandler;
    }
}
```

Guidelines:

- Cache delegates in private fields.
- Always clean up subscriptions in `OnDestroyed()`.
- Keep persistent customer configuration in `ConfigurePrefab(...)`, not in runtime event code.

## Pattern 3: Dealer NPC

Use when the NPC is recruitable, handles contracts, or participates in the dealer economy.

Minimum structure:

```csharp
public override bool IsDealer => true;

builder.EnsureDealer()
    .WithDealerDefaults(dd =>
    {
        dd.WithSigningFee(1000f)
            .WithCut(0.15f)
            .WithDealerType(DealerType.PlayerDealer)
            .WithHomeName("North Apartments");
    });
```

Recommended event wiring:

```csharp
private Action _dealerRecruitedHandler;

private void WireDealerEvents()
{
    if (Dealer == null)
        return;

    _dealerRecruitedHandler ??= HandleDealerRecruited;
    Dealer.OnRecruited -= _dealerRecruitedHandler;
    Dealer.OnRecruited += _dealerRecruitedHandler;
}

protected override void OnDestroyed()
{
    base.OnDestroyed();

    if (Dealer != null && _dealerRecruitedHandler != null)
    {
        Dealer.OnRecruited -= _dealerRecruitedHandler;
    }
}
```

Dealer checks:

- `IsDealer => true` is present.
- Dealer defaults are configured in prefab-time code.
- The schedule includes `EnsureDealSignal()`.
- Use `HandleDeal(...)` when you need an explicit dealer-handling slot.

## Pattern 4: Non-physical Contact NPC

Use for text-message contacts, phone contacts, story contacts, and other NPCs that do not need a world entity.

Recommended structure:

```csharp
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

Guidelines:

- Usually skip `WithSpawnPosition(...)`.
- Usually skip `WithSchedule(...)`.
- Consider `ClearConversationCategories()` for contacts that should not show the default badge.
- If message choices must survive load, implement `OnResponseLoaded(...)` and rebind callbacks there.

## Pattern 5: NPC as UI entry point

Use when talking to the NPC should open a UI, app, editor, or other interface.

Recommended flow:

1. Register the dialogue container in `OnCreated()`.
2. Do any required state changes in `OnChoiceSelected(...)`.
3. End or exit dialogue safely.
4. Open the UI after dialogue cleanup is in the correct state.

This keeps camera restoration, input focus, and override lifetime under control.

## Pattern 6: Styled narrative NPC

Use when the NPC should feel authored rather than purely functional.

Guidelines:

- Store key route points in `static readonly Vector3` fields.
- Make schedule choices reflect the character's role.
- Write dialogue that matches the role, not generic test text.
- Avoid maintaining two conflicting appearance definitions unless both are intentionally needed.

## Pattern 7: Conditional-presence NPC

Use when the NPC should only exist for certain story states, region states, or progression states.

Safe approach:

- Check the relevant external state early in `OnCreated()`.
- If the NPC should not continue, stop runtime initialization cleanly.

Be careful not to confuse:

- "this NPC should not exist right now"
- with
- "this NPC should exist but not yet be interactable"

If the NPC should simply be gated, prefer relationship locks, dialogue gates, or unlock conditions over destroying it.

## Stable architectural conclusions

1. Put one NPC class per role file under `NPCs/`.
2. Keep role-specific logic inside the NPC class instead of bloating `Core.cs`.
3. The most natural internal split for complex NPCs is: appearance, schedule, dialogue, customer/dealer, and event wiring.
4. For multi-capability NPCs, keep the logic cohesive inside the role rather than scattering it across unrelated services.

## Default implementation order

When the request is just "make a custom NPC," default to this order:

1. Decide whether it is physical or non-physical.
2. Decide whether it is plain, customer, dealer, or UI/story focused.
3. Write `ConfigurePrefab(...)` first.
4. Write `OnCreated()` second.
5. Add cleanup and save/load restoration last.

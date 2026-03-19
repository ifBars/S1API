# S1API Custom NPC Reference

## Coverage

This reference consolidates the most relevant guidance from:

- `custom-npcs.md`
- `basic-npc-creation.md`
- `appearance-customization.md`
- `customer-behavior.md`
- `dealer-system.md`
- `prefab-configuration.md`
- `runtime-management.md`
- `scheduling-system.md`

It also adds AvatarFramework-backed notes from:

- `AvatarFramework/Avatar.cs`
- `AvatarFramework/AvatarSettings.cs`
- `AvatarFramework/Customization/BasicAvatarSettings.cs`
- `AvatarFramework/Customization/CustomizationManager.cs`
- `AvatarFramework/Eye.cs`
- `AvatarFramework/EyeController.cs`
- `AvatarFramework/Eyebrow.cs`
- asset folders under `avatar/`

## Two-phase Model

S1API custom NPCs follow a strict split:

- `ConfigurePrefab(...)`: persistent defaults, prefab composition, save/load-safe configuration, and network-safe setup.
- `OnCreated()`: runtime wiring, avatar build, dialogue callbacks, event subscriptions, and behavior that depends on the instance already existing.

That split is the most important rule in the entire system.

## Minimal Patterns

### Physical NPC

```csharp
public sealed class MyFirstNpc : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var spawnPos = new Vector3(-50f, 1.06f, 70f);
        var hangoutPos = new Vector3(-28f, 1.06f, 62f);

        builder.WithIdentity("my_first_npc", "Alex", "Example")
            .WithSpawnPosition(spawnPos)
            .WithAppearanceDefaults(av =>
            {
                av.Gender = 0.5f;
                av.Height = 1.0f;
                av.Weight = 0.5f;
                av.HairPath = "Avatar/Hair/Spiky/Spiky";
            })
            .WithSchedule(plan =>
            {
                plan.WalkTo(hangoutPos, 900, faceDestinationDir: true);
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

### Non-physical contact

```csharp
public sealed class ContactNpc : NPC
{
    public override bool IsPhysical => false;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("contact_npc", "Unknown", "Contact")
            .WithIcon(null);
    }
}
```

Non-physical NPCs usually do not need `WithSpawnPosition(...)`, `WithSchedule(...)`, or `Schedule.Enable()`.

## Prefab-time Responsibilities

### Identity and icon

```csharp
builder.WithIdentity("my_custom_npc", "John", "Doe")
    .WithIcon(iconSprite);
```

Rules:

- `id` must be unique.
- `id` is part of save identity and system references.
- For new code, configure identity in `ConfigurePrefab(...)`, not with the obsolete constructor pattern.

### Spawn position

```csharp
builder.WithSpawnPosition(new Vector3(0, 0, 0));
builder.WithSpawnPosition(new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0));
```

Choose points that are on walkable surfaces and fit the planned route.

### Customer defaults

```csharp
builder.EnsureCustomer()
    .WithCustomerDefaults(cd =>
    {
        cd.WithSpending(150f, 600f)
            .WithOrdersPerWeek(1, 4)
            .WithPreferredOrderDay(Day.Friday)
            .WithOrderTime(1100)
            .WithStandards(CustomerStandard.VeryLow)
            .AllowDirectApproach(true)
            .GuaranteeFirstSample(true)
            .WithCallPoliceChance(0.15f)
            .WithMutualRelationRequirement(2.5f, 4.0f)
            .WithDependence(0.1f, 1.1f)
            .WithAffinities(new[]
            {
                (DrugType.Marijuana, 0.45f),
                (DrugType.Cocaine, -0.2f)
            })
            .WithPreferredProperties(Property.Munchies, Property.Energizing);
    });
```

Customer configuration belongs in `ConfigurePrefab(...)` only.

Runtime customer work is limited to events and basic actions such as:

- `Customer.OnUnlocked(...)`
- `Customer.OnDealCompleted(...)`
- `Customer.OnContractAssigned(...)`
- `Customer.ForceDealOffer()`
- `Customer.RequestProduct()`
- `Customer.SetAwaitingDelivery(true)`

### Dealer defaults

```csharp
public override bool IsDealer => true;

builder.EnsureDealer()
    .WithDealerDefaults(dd =>
    {
        dd.WithSigningFee(1000f)
            .WithCut(0.15f)
            .WithDealerType(DealerType.PlayerDealer)
            .WithHomeName("North Apartments")
            .AllowInsufficientQuality(false)
            .AllowExcessQuality(true)
            .WithCompletedDealsVariable("dealer_completed_deals");
    });
```

Dealer rules:

- Declare `IsDealer => true`.
- Configure dealer defaults in `ConfigurePrefab(...)`.
- For a functioning dealer route, include `plan.EnsureDealSignal()` in the schedule.
- Common runtime events are `Dealer.OnRecruited`, `Dealer.OnContractAccepted`, and `Dealer.OnRecommended`.

### Relationship defaults

```csharp
builder.WithRelationshipDefaults(r =>
{
    r.WithDelta(1.5f)
        .SetUnlocked(false)
        .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
        .WithConnectionsById("kyle_cooley", "ludwig_meyer");
});
```

### Schedule defaults

Prefer wrapper methods:

```csharp
builder.WithSchedule(plan =>
{
    plan.WalkTo(destination, 900)
        .StayInBuilding(building, 1000, 60)
        .UseVendingMachine(1400)
        .LocationDialogue(destination, 1900);
});
```

Wrapper preference order:

1. `WalkTo(...)`
2. `StayInBuilding(...)`
3. `UseVendingMachine(...)`
4. `LocationDialogue(...)`
5. `HandleDeal(...)` when it clearly matches the dealer role
6. `plan.Add(new ...Spec())` only for advanced cases

Schedule conclusions from the docs:

- `EnsureDealSignal()` is common for customers and effectively required for dealer contract handling.
- Use wrapper methods unless you specifically need spec-only options such as special seat lookup, complex GUID routing, or advanced parking setup.
- `SitAtSeatSet(...)` and `SitSpec` need `durationMinutes > 0` or the action will never trigger.

### Location-based actions

Before using arrival actions, add the matching prefab component:

```csharp
builder.EnsureSmokeBreak()
    .EnsureGraffiti()
    .EnsureDrinking()
    .EnsureItemHolding();
```

Then schedule them:

```csharp
builder.WithSchedule(plan =>
{
    plan.LocationBased(destination, 1200, 60)
        .Within(1.5f)
        .OnArriveSmokeBreak();
});
```

## Runtime Responsibilities

### Standard order

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    Appearance.Build();
    Schedule.Enable();
}
```

Add `Schedule.InitializeActions()` when the NPC needs explicit action initialization.

### Dialogue

```csharp
Dialogue.BuildAndRegisterContainer("ShopDialogue", c =>
{
    c.AddNode("ENTRY", "Welcome.", ch =>
    {
        ch.Add("BUY", "Buy", "BUY_NODE")
          .Add("LEAVE", "Leave", "EXIT");
    });
});

Dialogue.OnChoiceSelected("BUY", () =>
{
    Dialogue.JumpTo("ShopDialogue", "BUY_NODE");
});

Dialogue.UseContainerOnInteract("ShopDialogue");
```

Important rule:

- `Dialogue.StopOverride()` is safe from `OnChoiceSelected(...)`.
- Do not call `Dialogue.StopOverride()` from `OnNodeDisplayed(...)`, or you risk recursion and stack overflow.

### Event cleanup

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

### Save/load-sensitive lifecycle hooks

- `OnLoaded()`: restore runtime state after load.
- `OnResponseLoaded(Response response)`: reattach callbacks to saved text-message responses.

If the NPC is primarily message-driven, these hooks matter more than they do for a simple world-only NPC.

### Runtime state APIs

Common runtime properties and methods:

```csharp
string id = ID;
string fullName = FullName;
Vector3 position = Position;
Aggressiveness = 3f;
Region = Region.Northtown;

SendTextMessage("Hello!");
Goto(new Vector3(10, 0, 10));
ClearConversationCategories();
```

Use `ClearConversationCategories()` for story contacts, family/friends, informants, and other contacts that should not show Customer/Supplier/Dealer badges.

## AvatarFramework Appearance Constraints

These notes come from the underlying implementation, not just the docs.

### Core numeric fields

- `Gender`: no hard clamp in `Avatar.ApplyBodySettings`, but AvatarFramework treats it as a normalized value and `Avatar.IsMale()` uses `< 0.5f`. Use `0.0f` to `1.0f`.
- `Weight`: no hard clamp in `Avatar.ApplyBodySettings`; it is applied as blendshape weight `settings.Weight * 100f`. Use `0.0f` to `1.0f`.
- `Height`: applied directly to `transform.localScale` with no clamp. S1API defaults use about `0.98f`, and random generation uses `0.8f` to `1.2f`. Treat `0.8f` to `1.2f` as the safe practical range.
- `PupilDilation`: EyeController exposes `[Range(0f, 1f)]`. Use `0.0f` to `1.0f`.
- `EyeLidRestingStateLeft/Right`: `Eye.EyeLidConfiguration` marks both lid values with `[Range(0f, 1f)]`. Keep both lid values within `0.0f` to `1.0f`.
- `EyebrowRestingHeight`: runtime code clamps this to `-1.1f` through `1.5f`.
- `EyebrowScale`, `EyebrowThickness`, `EyebrowRestingAngle`: no useful hard clamp was found in the runtime application path; use conservative normalized-looking values near existing defaults unless you intentionally want stylized results.

### Defaults

Observed defaults from AvatarFramework and S1API code paths:

- `Height`: `0.98f` in AvatarFramework defaults, `1.0f` in several runtime examples
- `Gender`: `0.0f` default in AvatarFramework fallback settings
- `Weight`: `0.4f` default in AvatarFramework fallback settings
- `PupilDilation`: `1.0f` in AvatarFramework defaults, `0.5f` in some EyeController state
- eyelids: both top and bottom default to `0.5f`

### Layer and accessory limits

Practical limits for NPC appearance work:

- Face layers: 6
- Body layers: 6
- Accessory layers: 9

Those limits line up with `NPCAppearance` constants and the shape of `AvatarSettings` accessors.

### Asset paths

Asset paths are loaded with `Resources.Load(...)`, so use resource-style paths with no extension.

Valid path examples:

- `Avatar/Hair/Spiky/Spiky`
- `Avatar/Layers/Face/Face_Agitated`
- `Avatar/Layers/Top/T-Shirt`
- `Avatar/Layers/Bottom/Jeans`
- `Avatar/Accessories/Feet/Sneakers/Sneakers`

Use the actual `avatar/` folder structure as the source of truth when unsure about naming.

### Colors

- `SkinColor`: commonly passed as `Color32` with 0-255 RGBA style values.
- most other tints such as `HairColor` and `EyeBallTint`: commonly passed as `Color` with 0-1 float channels.

### Build requirement

`NPCAppearance.Build()` triggers mugshot generation and final application. If you skip it, the NPC appearance is incomplete.

## Validation Checklist

For every custom NPC change, check:

1. The NPC spawns in the intended place.
2. The avatar builds successfully and the mugshot/icon looks correct.
3. Dialogue opens the expected container.
4. The schedule actually starts and reaches the expected actions.
5. Customer or dealer behavior works as configured.
6. Save/load restores runtime behavior correctly.
7. Saved message responses still work after load via `OnResponseLoaded(...)`.

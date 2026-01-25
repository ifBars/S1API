# Basic NPC Creation

This page gets you from "empty class" to a physical NPC that spawns, has an avatar, and follows a simple schedule.

The two phases to remember:

- `ConfigurePrefab(...)`: saved defaults (identity, relationships, schedules, customer/dealer defaults)
- `OnCreated()`: runtime wiring (build avatar, dialogue callbacks, event subscriptions)

## Step 1: Create the NPC class

Create a class that inherits `S1API.Entities.NPC`.

```csharp
using S1API.Entities;
using UnityEngine;

public sealed class MyFirstNpc : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        // Step 2
    }

    protected override void OnCreated()
    {
        // Step 3
    }
}
```

## Step 2: Configure the prefab (identity, spawn, schedule)

In `ConfigurePrefab(...)`, set:

1. Identity: `WithIdentity(id, firstName, lastName)`
2. Spawn: `WithSpawnPosition(position[, rotation])`
3. (Physical NPCs) Schedule: `WithSchedule(...)`
4. (Optional) Appearance defaults: `WithAppearanceDefaults(...)`

```csharp
using S1API.Entities;
using S1API.Entities.Schedule;
using UnityEngine;

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
}
```

Notes:

- Treat the `id` passed to `WithIdentity(...)` as stable save data; changing it later effectively creates a different NPC.
- If your NPC is not physical (`IsPhysical => false`), skip scheduling and focus on messaging/dialogue.

For the full set of builder options (customer, relationship, dealer, inventory, schedule specs): `S1API/docs/prefab-configuration.md`.

## Step 3: Runtime initialization (build avatar, enable schedule)

In `OnCreated()`, do runtime work:

- call `base.OnCreated()`
- build the avatar (`Appearance.Build()`)
- enable systems you configured (typically `Schedule.Enable()` for physical NPCs)

```csharp
protected override void OnCreated()
{
    base.OnCreated();

    // Applies the appearance defaults configured in ConfigurePrefab.
    Appearance.Build();

    // Starts the schedule engine for this NPC.
    Schedule.Enable();
}
```

If you wire events (customer/dealer/etc), unsubscribe in `OnDestroyed()`; see `S1API/docs/runtime-management.md`.

## Step 4 (optional): Add interaction

Once the NPC exists in-world, add dialogue. Keep the logic small here and lean on the dedicated page:

- `S1API/docs/dialogue-system.md`

## Step 5 (optional): Make them a customer or dealer

- Customer NPCs: `builder.EnsureCustomer().WithCustomerDefaults(...)` (see `S1API/docs/customer-behavior.md`)
- Dealer NPCs: `public override bool IsDealer => true;` + `builder.EnsureDealer().WithDealerDefaults(...)` (see `S1API/docs/dealer-system.md`)

## Example NPCs

If you prefer starting from a working, full-featured NPC, use the **[S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)**:

- **[ExamplePhysicalNPC1](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC1.cs)**: physical customer NPC (appearance, dialogue, inventory, schedule)
- **[ExamplePhysicalNPC2](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC2.cs)**: customer events and recommending a dealer after a completed deal
- **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)**: full dealer setup (defaults, events, schedule)
- **[CharacterCustomizerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/CharacterCustomizerNPC.cs)**: UI integration (opens the character creator from NPC dialogue)

## Next Steps

- Prefab options: `S1API/docs/prefab-configuration.md`
- Scheduling details: `S1API/docs/scheduling-system.md`
- Dialogue details: `S1API/docs/dialogue-system.md`
- Lifecycle/events: `S1API/docs/runtime-management.md`

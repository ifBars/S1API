# Casino Slot Machines

The S1API provides a modder-facing API for making NPCs interact with slot machines in the casino. This system handles cash management, animations, and outcome determination automatically.

## Overview

The slot machine system allows NPCs to:
- Walk to slot machines and play them
- Use cash from their inventory to place bets
- Receive winnings back into their inventory
- Trigger all the visual and audio effects of the slot machine

## Basic Usage

### Schedule Integration

The easiest way to make an NPC use a slot machine is through their schedule. There are several methods depending on how long you want the NPC to gamble:

#### Single Spin

```csharp
using S1API.Entities.Schedule;
using UnityEngine;

protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithSchedule(plan =>
    {
        plan
            // Use slot machine once at 16:30 with $10 bet
            .UseSlotMachine(1630, new Vector3(23.4776f, 1.8546f, 95.6571f), betAmount: 10);
    });
}
```

#### Multiple Spins

```csharp
// Play 5 spins, waiting 15 seconds between each
.UseSlotMachineMultipleTimes(
    startTime: 1630,
    machinePosition: new Vector3(23.4776f, 1.8546f, 95.6571f),
    spinCount: 5,
    betAmount: 10,
    timeBetweenSpins: 15f
);
```

#### Gamble Until Specific Time

```csharp
// Gamble from 16:30 until 18:00 (6:00 PM)
.UseSlotMachineUntilTime(
    startTime: 1630,
    endTime: 1800,
    machinePosition: new Vector3(23.4776f, 1.8546f, 95.6571f),
    betAmount: 10,
    timeBetweenSpins: 10f
);
```

#### Gamble Until Broke

```csharp
// Gamble until the NPC runs out of money
.UseSlotMachineUntilBroke(
    startTime: 1630,
    machinePosition: new Vector3(23.4776f, 1.8546f, 95.6571f),
    betAmount: 10,
    timeBetweenSpins: 10f
);
```

### Manual Usage

You can also make an NPC use a slot machine programmatically:

```csharp
using S1API.Casino;
using UnityEngine;

// Make NPC use nearest slot machine to this position
Vector3 slotMachinePosition = new Vector3(23.4776f, 1.8546f, 95.6571f);
int betAmount = 10; // $10 bet

bool success = SlotMachineHelper.UseSlotMachine(
    npc, 
    slotMachinePosition, 
    betAmount, 
    maxSearchDistance: 5f
);

if (success)
{
    MelonLogger.Msg($"{npc.ID} is using the slot machine!");
}
```

## Cash Management

NPCs use cash items from their inventory to place bets. The system automatically:
- Checks if the NPC has enough cash
- Removes the bet amount from inventory
- Adds winnings back to inventory

### Giving NPCs Cash

Ensure your NPC has cash in their inventory:

```csharp
builder.WithInventoryDefaults(inv =>
{
    // Give NPC $50-$500 cash each night
    inv.WithRandomCash(min: 50, max: 500);
});
```

### Checking NPC Cash

You can check how much cash an NPC has:

```csharp
using S1API.Casino;

int cash = SlotMachineHelper.GetNPCCash(npc);
MelonLogger.Msg($"{npc.ID} has ${cash}");
```

## Slot Machine Positions

To find slot machine positions in the casino:

1. **In-game**: Use the game's coordinate display or a debug mod
2. **Common casino slot machine positions**:
   - `Vector3(23.4776f, 1.8546f, 95.6571f)` - Near entrance
   - `Vector3(26.2f, 1.8546f, 95.6f)` - Middle row
   - `Vector3(29.0f, 1.8546f, 95.6f)` - Far row

The `maxSearchDistance` parameter (default 5.0) determines how far from the specified position the system will search for a slot machine.

## Bet Amounts

Common bet amounts match the slot machine's preset values:
- `5` - $5 bet
- `10` - $10 bet (default)
- `25` - $25 bet
- `50` - $50 bet
- `100` - $100 bet

## Win Outcomes

The slot machine has five possible outcomes:

| Outcome | Description | Multiplier | Example ($10 bet) |
|---------|-------------|------------|-------------------|
| Jackpot | Three sevens | 100x | $1,000 |
| Big Win | Three bells | 25x | $250 |
| Small Win | Three matching fruits | 10x | $100 |
| Mini Win | Any three fruits | 2x | $20 |
| No Win | No match | 0x | $0 |

## Session Modes

The slot machine system supports different gambling session behaviors:

| Mode | Description | Use Case |
|------|-------------|----------|
| `SingleSpin` | Play once and stop | Quick gambling action |
| `SpinCount` | Play a specific number of times | Controlled gambling session |
| `UntilTime` | Play until a time is reached | Time-based gambling session |
| `UntilBroke` | Play until out of money | Gambling addiction behavior |
| `UntilTimeOrBroke` | Play until time OR out of money | Realistic gambling with limits |

## Advanced Examples

### Example 1: Casual Gambler

An NPC that gambles moderately:

```csharp
using S1API.Entities;
using S1API.Entities.Schedule;
using S1API.Map;
using S1API.Map.Buildings;
using UnityEngine;

public class CasualGamblerNPC : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var home = Building.Get<PlayerHome>();
        
        builder.WithIdentity("casual_joe", "Joe", "The Casual Gambler")
            .WithAppearanceDefaults(av =>
            {
                // ... appearance setup ...
            })
            .WithSpawnPosition(new Vector3(-10f, 0f, 10f))
            .WithInventoryDefaults(inv =>
            {
                // Give Joe moderate gambling money
                inv.WithRandomCash(min: 100, max: 300)
                   .WithClearInventoryEachNight(false); // Keep winnings!
            })
            .WithSchedule(plan =>
            {
                plan
                    // Morning at home
                    .StayInBuilding(home, 700, durationMinutes: 540) // 7:00 AM - 4:00 PM
                    // Walk to casino
                    .WalkTo(new Vector3(23.5696f, 1.865f, 89.4422f), 1600) // 4:00 PM
                    // Play 10 spins with $5 bets
                    .UseSlotMachineMultipleTimes(
                        startTime: 1610,
                        machinePosition: new Vector3(23.4776f, 1.8546f, 95.6571f),
                        spinCount: 10,
                        betAmount: 5,
                        timeBetweenSpins: 15f
                    )
                    // Leave casino
                    .WalkTo(new Vector3(-10f, 0f, 10f), 1700); // 5:00 PM
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

### Example 2: Problem Gambler

An NPC with gambling addiction:

```csharp
public class ProblemGamblerNPC : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var home = Building.Get<PlayerHome>();
        
        builder.WithIdentity("problem_pete", "Pete", "The Problem Gambler")
            .WithAppearanceDefaults(av =>
            {
                // ... appearance setup ...
            })
            .WithSpawnPosition(new Vector3(-10f, 0f, 10f))
            .WithInventoryDefaults(inv =>
            {
                // Pete has more money but will gamble it all
                inv.WithRandomCash(min: 500, max: 1500)
                   .WithClearInventoryEachNight(false);
            })
            .WithSchedule(plan =>
            {
                plan
                    // Skip morning routine, go straight to casino
                    .WalkTo(new Vector3(23.5696f, 1.865f, 89.4422f), 1000) // 10:00 AM
                    // Gamble from 10:00 AM until 11:00 PM or until broke
                    .UseSlotMachineUntilTime(
                        startTime: 1010,
                        endTime: 2300, // 11:00 PM
                        machinePosition: new Vector3(23.4776f, 1.8546f, 95.6571f),
                        betAmount: 25,
                        timeBetweenSpins: 8f,
                        stopIfBroke: true // Stop when out of money
                    )
                    // Walk home (if any money left)
                    .WalkTo(new Vector3(-10f, 0f, 10f), 2300);
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

### Example 3: Lucky Streak Strategy

An NPC that tries different machines:

```csharp
.WithSchedule(plan =>
{
    plan
        .WalkTo(new Vector3(23.5696f, 1.865f, 89.4422f), 1600)
        // Try machine 1 for 3 spins
        .UseSlotMachineMultipleTimes(1610, new Vector3(23.4776f, 1.8546f, 95.6571f), 
            spinCount: 3, betAmount: 10)
        // Move to machine 2 for 3 spins
        .UseSlotMachineMultipleTimes(1630, new Vector3(26.2f, 1.8546f, 95.6f), 
            spinCount: 3, betAmount: 10)
        // Try machine 3 for 3 spins
        .UseSlotMachineMultipleTimes(1650, new Vector3(29.0f, 1.8546f, 95.6f), 
            spinCount: 3, betAmount: 10)
        .WalkTo(new Vector3(-10f, 0f, 10f), 1720);
});
```

## Troubleshooting

### NPC doesn't use the slot machine

**Check:**
1. Does the NPC have enough cash? Use `GetNPCCash()` to verify
2. Is the slot machine position correct? The NPC searches within `maxSearchDistance`
3. Is another NPC already using that machine? Slot machines can only be used by one entity at a time
4. For time-based sessions, ensure `endTime` is after `startTime`

### NPC walks to wrong machine

**Solution:**
- Reduce `maxSearchDistance` to ensure only the intended machine is found
- Verify the `machinePosition` coordinates are accurate

### NPC stops gambling early

**Check:**
- For `UntilTime` mode: Verify `endTime` is set correctly
- For `SpinCount` mode: Check if NPC ran out of cash before completing all spins
- For `UntilTimeOrBroke` mode: NPC stops at whichever comes first (time or broke)
- Check console logs for "Out of money" or similar messages

### Cash not being removed/added

**Check:**
- Ensure the NPC has cash items (not just money in a dealer balance)
- Verify inventory is properly initialized with `WithInventoryDefaults()`
- Check logs for any error messages from `SlotMachineHelper`

### NPC gambles too fast/slow

**Solution:**
- Adjust `timeBetweenSpins` parameter (default is 10 seconds)
- Lower values = faster gambling
- Higher values = more realistic pacing

## Notes

- Slot machines are networked objects - the spin will be visible to all players
- The outcome is determined server-side for fairness
- NPCs can only use slot machines when they're not already spinning
- Cash transactions are networked to keep inventory synchronized


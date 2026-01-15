# Leveling 
```csharp
using S1API.Leveling;
```

The Leveling module exposes the player progression system, including  
ranks, tiers, XP, unlockables, and progression events.
---

## Overview

The Leveling module allows you to:

- Read the players current rank, tier, and XP
- Award XP to the player
- Track XP changes and rank-ups via events
- Work with combined rank + tier values
- Register and query rank-based unlockables
- Safely interact with progression through a static API

---

## Core Types

## Rank (enum)

Represents the base player rank.

```csharp
public enum Rank
{
    StreetRat,
    Hoodlum,
    Peddler,
    Hustler,
    Bagman,
    Enforcer,
    ShotCaller,
    BlockBoss,
    Underlord,
    Baron,
    Kingpin
}
```

### Notes

- ranks have only **5 tiers**
- `Kingpin` has **unlimited tiers**

---

## FullRank (struct)

Represents a **rank + tier combination**.

```csharp
public readonly struct FullRank
```

### Constructor

```csharp
public FullRank(Rank rank, int tier)
```

| Parameter | Description |
|----------|-------------|
| `rank`   | Player rank |
| `tier`   | Tier within the rank (minimum 1) |

If `tier < 1`, it is clamped to `1`.

---

### Properties

| Property | Type | Description |
|---------|------|-------------|
| `Rank`  | `Rank` | Rank component |
| `Tier`  | `int`  | Tier component |

---

### Methods

#### NextRank()

```csharp
public FullRank NextRank()
```

Returns the next progression step:

- Tier 1 → 5 → next Rank
---

#### ToFloat()

```csharp
public float ToFloat()
```

Converts the rank into a float value:

```
Rank + Tier / 5
```

Useful for interpolation and progress bars.

---

#### GetRankIndex()

```csharp
public int GetRankIndex()
```

Returns a linear index suitable for UI:

```
Rank * 5 + (Tier - 1)
```

---

#### ToString()

Returns a human-readable name:

```
Street Rat I
Shot Caller IV
Block Boss V
```

---

### Comparison Support

`FullRank` supports:

- `> < >= <= == !=`
- `IComparable<FullRank>`
- `IEquatable<FullRank>`

Example:

```csharp
if (LevelManager.CurrentRank >= new FullRank(Rank.Hustler, 3))
{
    UnlockFeature();
}
```

---

## Unlockable

Represents a UI or gameplay element unlocked at a specific rank.

```csharp
public sealed class Unlockable
```

### Constructor

```csharp
public Unlockable(FullRank rank, string title, Sprite icon)
```

| Parameter | Description |
|----------|-------------|
| `rank`   | Required rank |
| `title`  | Display title |
| `icon`   | UI icon |

---

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Rank`   | `FullRank` | Rank requirement |
| `Title`  | `string`   | Display title |
| `Icon`   | `Sprite`   | Associated icon |

---

## LevelManager

Primary entry point for player progression.

```csharp
public static class LevelManager
```

---

## Events

### OnXPChanged

```csharp
public static event Action<FullRank, FullRank>? OnXPChanged;
```

Triggered **on every XP update**, even if the rank/tier did not change.

---

### OnRankUp

```csharp
public static event Action<FullRank, FullRank>? OnRankUp;
```

Triggered **only when the rank or tier actually increases**.

---

## Usage Example

```csharp
using UnityEngine;
using S1API.Leveling;

public class Example
{
    public void Initialize()
    {
        // Ensure LevelManager is initialized
        if (!LevelManager.Exists)
            return;

        // Subscribe to XP change events
        LevelManager.OnXPChanged += OnXPChanged;

        // Subscribe to rank-up events
        LevelManager.OnRankUp += OnRankUp;

        // Read current progression state
        Rank currentRank = LevelManager.Rank;
        int currentTier = LevelManager.Tier;
        int currentXP = LevelManager.XP;
        int totalXP = LevelManager.TotalXP;
        float xpToNext = LevelManager.XPToNextTier;

        FullRank fullRank = LevelManager.CurrentRank;

        // Add XP
        LevelManager.AddXP(250);

        // Calculate rank information
        int xpForTier = LevelManager.GetXPForTier(currentRank);
        FullRank rankFromXP = LevelManager.GetFullRankForXP(totalXP);
        int totalXpForRank = LevelManager.GetTotalXPForRank(fullRank);

        // Order limit multiplier
        float orderMultiplier = LevelManager.GetOrderLimitMultiplier(fullRank);

        // Work with FullRank helpers
        FullRank nextRank = fullRank.NextRank();
        float rankAsFloat = fullRank.ToFloat();
        int rankIndex = fullRank.GetRankIndex();

        Log.Msg($"Next Rank: {nextRank}");
        Log.Msg($"Rank Float: {rankAsFloat}, Index: {rankIndex}");

        // Create and register unlockables
        var unlockable = new Unlockable(
            new FullRank(Rank.Peddler, 2),
            "New Dealer Slot",
            null // u can add there Icon Sprite if u have
        );

        LevelManager.AddUnlockable(unlockable);

        // Query unlockables for a specific rank
        foreach (var u in LevelManager.GetUnlockables(fullRank))
        {
            Log.Msg($"Unlockable available: {u.Title} at {u.Rank}");
        }

        // Rank comparison
        if (fullRank >= new FullRank(Rank.Hustler, 1))
        {
            UnlockAdvancedFeature();
        }
    }

    private void OnXPChanged(FullRank oldRank, FullRank newRank)
    {
        Log.Msg($"XP changed: {oldRank} -> {newRank}");
    }

    private void OnRankUp(FullRank oldRank, FullRank newRank)
    {
        Log.Msg($"Rank UP: {oldRank} -> {newRank}");
    }

    //private void UnlockAdvancedFeature()
    //{
    //    Log.Msg("Advanced feature unlocked!"); js kidding :)
    //} 
}
```


---
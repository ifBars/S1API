---
title: "Levelling / XP System"
description: "Reference for the levelling system — LevelManager, ERank, XP gain, and rank progression"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Levelling / XP System

## LevelManager (`Il2CppScheduleOne.Levelling.LevelManager`)
`NetworkSingleton<LevelManager>`, ISaveable. Manages player rank, tiers, and XP.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `TIERS_PER_RANK` | 5 | Tiers needed to advance rank |
| `XP_PER_TIER_MIN` | 200 | XP needed for tier 0 |
| `XP_PER_TIER_MAX` | 2500 | XP needed for max rank tier |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Rank` | `ERank` (get) | Current rank |
| `Tier` | `int` (get) | Current tier (1-5) |
| `XP` | `int` (get) | Current XP |
| `TotalXP` | `int` (get/set) | Lifetime XP |
| `XPToNextTier` | `float` (get) | XP needed for next tier |

### Events
| Event | Args | Description |
|-------|------|-------------|
| `onRankUp` | `Action<FullRank, FullRank>` | Rank increased |
| `onRankChanged` | `Action<FullRank, FullRank>` | Any rank change |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Unlockables` | `Dictionary<FullRank, List<Unlockable>>` | Unlocks per rank |

### Methods
| Method | Description |
|--------|-------------|
| `AddXP(int xp)` | ServerRpc — add XP |
| `GetFullRank()` | Returns `FullRank` (Rank + Tier) |
| `SetRank(ERank, int tier, int xp, int totalXP)` | Server-only, set data |

### XP Calculation
```
XPToNextTier = Mathf.Round(
    Mathf.Lerp(200f, 2500f, (float)Rank / (float)rankCount) / 25f
) * 25f
```

---

## ERank Enum
| Value | Index | Description |
|-------|-------|-------------|
| `Street_Rat` | 0 | Street Rat |
| `Hoodlum` | 1 | Hoodlum |
| `Peddler` | 2 | Peddler |
| `Hustler` | 3 | Hustler |
| `Bagman` | 4 | Bagman |
| `Enforcer` | 5 | Enforcer |
| `Shot_Caller` | 6 | Shot Caller |
| `Block_Boss` | 7 | Block Boss |
| `Underlord` | 8 | Underlord |
| `Baron` | 9 | Baron |
| `Kingpin` | 10 | Kingpin |

---

## FullRank (`Il2CppScheduleOne.Levelling.FullRank`)
Combines rank + tier into one object.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Rank` | `ERank` | Current rank |
| `Tier` | `int` | Current tier (1-5) |

### Methods
| Method | Description |
|--------|-------------|
| `GetDisplayString()` | Formatted rank display |
| `GetRankUpXP()` | XP needed for next rank |

---

## RankData (`Il2CppScheduleOne.Levelling.RankData`)
Save data class for rank state.

---

## Unlockable (`Il2CppScheduleOne.Levelling.Unlockable`)
Represents something unlocked at a specific rank.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `UnlockRank` | `FullRank` | Required rank |
| `UnlockName` | `string` | Description |
| `IsUnlocked` | `bool` | Check if available |

---

## Usage Patterns

### Adding XP
```csharp
if (InstanceFinder.IsServer)
{
    Singleton<LevelManager>.Instance.AddXP(250);
}
```

### Getting current rank
```csharp
ERank rank = Singleton<LevelManager>.Instance.Rank;
int tier = Singleton<LevelManager>.Instance.Tier;
int xp = Singleton<LevelManager>.Instance.XP;
float toNext = Singleton<LevelManager>.Instance.XPToNextTier;
```

### Checking if an unlock is available
```csharp
FullRank currentRank = Singleton<LevelManager>.Instance.GetFullRank();
foreach (var kvp in Singleton<LevelManager>.Instance.Unlockables)
{
    if (kvp.Key.Rank <= currentRank.Rank && kvp.Key.Tier <= currentRank.Tier)
    {
        // This unlock is available
        foreach (Unlockable unlockable in kvp.Value)
        {
            MelonLogger.Msg($"Unlocked: {unlockable.UnlockName}");
        }
    }
}
```

### Listening for rank up
```csharp
Singleton<LevelManager>.Instance.onRankUp += (oldRank, newRank) =>
{
    MelonLogger.Msg($"Ranked up from {oldRank.Rank} to {newRank.Rank}!");
};
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Progression.GetLevel()` | Get current level |
| `Api.Progression.GetXP()` | Get XP points |
| `Api.Progression.AddXP()` | Add XP |
| `Api.Progression.GetRank()` | Get current rank |

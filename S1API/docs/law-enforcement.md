# Law Enforcement System

S1API provides comprehensive control over the law enforcement system, including checkpoint management, police dispatch, wanted levels, and law enforcement intensity that controls automatic activities.

## Overview

The law enforcement system consists of several interconnected components:

- **Checkpoints** - Road checkpoints with officers that stop and search vehicles
- **Law Enforcement Intensity** - A 1-10 scale that controls automatic police activities
- **Automatic Evaluation System** - Runs every in-game minute to activate checkpoints, patrols, and sentries
- **Police Dispatch** - Systems for calling police and managing wanted levels
- **Patrols** - Foot and vehicle patrols (controlled by intensity and time)
- **Curfews** - Time-based restrictions with enforcement

## Understanding the Automatic System

**IMPORTANT**: The game includes an automatic checkpoint evaluation system that runs every in-game minute. This system will automatically enable or disable checkpoints based on several conditions:

- **Law Enforcement Intensity** (1-10 scale) - Higher values trigger more activities
- **Time of Day** - Checkpoints have configured start/end times
- **Day of the Week** - Different activity settings for each day
- **Curfew Status** - Some checkpoints only activate during curfew
- **Player Distance** - Checkpoints won't spawn near the player (50+ units away)
- **Officer Availability** - Requires officers in the police station pool

### Why Checkpoints Re-enable Automatically

When you disable a checkpoint using `CheckpointManager.SetCheckpointEnabled()`, the automatic evaluation system may re-enable it on the next game minute if conditions are met. This is by design - the game's law enforcement system is meant to be dynamic and responsive to the current game state.

**Solution**: To prevent automatic re-enabling, set the law enforcement intensity to a low value (1-4) using `LawController.SetIntensityLevel(1)`. Checkpoints require intensity >= 5 (default) to activate automatically.

## Documentation Structure

The Law Enforcement system is documented across multiple focused pages:

### Core Systems
- **[Checkpoint Management](checkpoint-management.md)** - Controlling checkpoints and querying their state
- **[Law Enforcement Intensity](law-enforcement-intensity.md)** - Managing intensity levels and automatic activities
- **[Police Dispatch](police-dispatch.md)** - Calling police, managing wanted levels, patrols, and pursuit systems

## Quick Start

Here's a minimal example to disable automatic checkpoint activation:

```csharp
using S1API.Law;

// Set intensity to minimum to prevent automatic activities
LawController.SetIntensityLevel(1);

// Disable all checkpoints
CheckpointManager.DisableAllCheckpoints();
```

## Namespace

```csharp
using S1API.Law;
```

## Related Systems

- **[Custom NPCs](custom-npcs.md)** - For working with police officer NPCs
- **[Quests](quests-system.md)** - Create missions involving law enforcement
- **[Save System](save-system.md)** - Persist custom law enforcement states

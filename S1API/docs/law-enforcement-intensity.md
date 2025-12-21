# Law Enforcement Intensity

Law enforcement intensity is a value from 1-10 that determines which automatic activities are enabled. Higher values trigger more aggressive police presence, including automatic checkpoint activation, increased patrols, and stricter enforcement.

## Overview

The intensity system controls the game's automatic law enforcement evaluation system, which runs every in-game minute to activate:

- Road checkpoints
- Foot patrols
- Vehicle patrols
- Sentry positions
- Curfew enforcement

### Intensity Level Guidelines

- **1-4**: Low enforcement - Automatic checkpoints and patrols are disabled (requires intensity >= 5)
- **5-7**: Medium enforcement - Checkpoints and patrols become active based on time/conditions
- **8-10**: High enforcement - Aggressive police presence, frequent checkpoints and patrols

**Note**: Checkpoints have a default `IntensityRequirement` of 5, meaning they won't activate automatically unless intensity is 5 or higher. This requirement can vary per checkpoint configuration.

## Checking Intensity

```csharp
using S1API.Law;

// Get current intensity level (1-10)
int intensity = LawController.Intensity;

// Get internal intensity (0.0-1.0 normalized value)
float internalIntensity = LawController.InternalIntensity;
```

## Changing Intensity

### Adjusting Intensity

```csharp
// Change intensity by an amount (can be negative)
LawController.ChangeIntensity(0.1f);  // Increase slightly
LawController.ChangeIntensity(-0.2f); // Decrease
```

### Setting Intensity Level

```csharp
// Set intensity to specific level (1-10)
LawController.SetIntensityLevel(3);  // Low enforcement
LawController.SetIntensityLevel(7);  // High enforcement
```

### Setting Internal Intensity

```csharp
// Set internal intensity directly (0.0-1.0)
LawController.SetInternalIntensity(0.0f);  // Minimum
LawController.SetInternalIntensity(1.0f);  // Maximum
```

## Preventing Automatic Checkpoint Activation

To prevent checkpoints from automatically enabling, set the intensity to a low value:

```csharp
// Set intensity to minimum to prevent automatic activities
LawController.SetIntensityLevel(1);

// Now you can safely disable checkpoints
CheckpointManager.DisableAllCheckpoints();
```

**This is the recommended approach** instead of constantly fighting the automatic system by disabling checkpoints every frame.

## Working With the System

### Dynamic Behavior Based on Intensity

```csharp
// Adjust checkpoint strategy based on intensity
int intensity = LawController.Intensity;
if (intensity >= 7)
{
    // High enforcement - enable strategic checkpoints
    CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Docks, true, 4);
}
else if (intensity <= 3)
{
    // Low enforcement - disable all
    CheckpointManager.DisableAllCheckpoints();
}
```

### Best Practice: Adjust Intensity, Don't Fight It

```csharp
// BAD: Fighting the automatic system
void Update()
{
    // This will be overridden every game minute
    CheckpointManager.DisableAllCheckpoints();
}

// GOOD: Adjusting the system's behavior
void DisableAutoCheckpoints()
{
    LawController.SetIntensityLevel(1);
    CheckpointManager.DisableAllCheckpoints();
}
```

## How Intensity Changes

### Automatic Increase

Intensity increases automatically each day by a configurable amount (default: 0.15 per day, equivalent to ~1.5 intensity levels per day). This means intensity will naturally rise from 1 to 10 over time unless manually controlled.

### Manual Control

You can override the automatic increase by manually setting the intensity level:

```csharp
// Set to a specific level (1-10)
LawController.SetIntensityLevel(3);

// Adjust by amount (can be negative to decrease)
LawController.ChangeIntensity(-0.2f);
```

## Constants

```csharp
// Intensity bounds
int minIntensity = LawController.MinIntensity;  // 1
int maxIntensity = LawController.MaxIntensity;  // 10

// Note: DailyIntensityDrain constant exists but intensity actually increases over time
```

## Advanced: Activity Settings Override

For advanced use cases, you can override the entire automatic activity system:

```csharp
// WARNING: Advanced feature - requires deep understanding of game systems
// This disables day-based activity settings

// Clear any override and return to normal operation
LawController.ClearActivitySettingsOverride();

// Check if override is active
bool usingOverride = LawController.IsUsingOverrideSettings;
```

**Note**: Overriding activity settings with custom settings requires working with internal game types and is not recommended for most mods. Instead, use intensity level control.

## Example: Intensity-Based Checkpoint Control

```csharp
using MelonLoader;
using S1API.Law;
using UnityEngine;

public class IntensityController : MelonMod
{
    public override void OnUpdate()
    {
        // Hotkey: Press F8 to disable all checkpoints and set low intensity
        if (Input.GetKeyDown(KeyCode.F8))
        {
            DisableAllLawEnforcement();
        }

        // Hotkey: Press F9 to enable high enforcement
        if (Input.GetKeyDown(KeyCode.F9))
        {
            EnableHighEnforcement();
        }
    }

    private void DisableAllLawEnforcement()
    {
        // Set intensity to minimum first
        LawController.SetIntensityLevel(1);
        
        // Then disable checkpoints
        CheckpointManager.DisableAllCheckpoints();
        
        LoggerInstance.Msg("All law enforcement disabled (intensity set to 1)");
    }

    private void EnableHighEnforcement()
    {
        // Set high intensity
        LawController.SetIntensityLevel(8);
        
        // Enable checkpoints with 3 officers each
        CheckpointManager.EnableAllCheckpoints(3);
        
        LoggerInstance.Msg("High enforcement enabled (intensity set to 8)");
    }
}
```


# Checkpoint Management

Checkpoints are road barriers with police officers that stop and search vehicles. This guide covers enabling/disabling checkpoints, querying their state, and understanding checkpoint behavior.

## Checkpoint Locations

There are four checkpoint locations in the game:

- `CheckpointLocation.Western` - Western checkpoint
- `CheckpointLocation.Docks` - Docks checkpoint
- `CheckpointLocation.NorthResidential` - North Residential checkpoint
- `CheckpointLocation.WestResidential` - West Residential checkpoint

## Enabling and Disabling Checkpoints

### Basic Usage

```csharp
using S1API.Law;

// Enable a checkpoint with 2 officers (default)
CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Western, true);

// Enable with 4 officers
CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Docks, true, 4);

// Disable a checkpoint
CheckpointManager.SetCheckpointEnabled(CheckpointLocation.NorthResidential, false);

// Enable all checkpoints
CheckpointManager.EnableAllCheckpoints(2);

// Disable all checkpoints
CheckpointManager.DisableAllCheckpoints();
```

**Note**: Officers are pulled from the nearest police station's officer pool. If insufficient officers are available, fewer than requested will be assigned.

### Preventing Automatic Re-enabling

The automatic evaluation system may re-enable checkpoints every game minute if conditions are met. Checkpoints require law enforcement intensity >= 5 (default) to activate automatically. To prevent this:

```csharp
// Set intensity below the checkpoint requirement (default is 5)
LawController.SetIntensityLevel(1);

// Then disable checkpoints
CheckpointManager.DisableAllCheckpoints();
```

**Note**: Intensity increases automatically each day (~0.15 per day by default), so you may need to periodically reset it if you want to keep checkpoints disabled long-term.

## Checking Checkpoint State

### Basic State Queries

```csharp
// Check if a checkpoint is enabled
bool isEnabled = CheckpointManager.IsCheckpointEnabled(CheckpointLocation.Western);

// Get checkpoint position
Vector3 position = CheckpointManager.GetCheckpointPosition(CheckpointLocation.Docks);

// Get number of assigned officers
int officerCount = CheckpointManager.GetAssignedOfficerCount(CheckpointLocation.Western);

// Check gate status
bool gate1Open = CheckpointManager.IsGate1Open(CheckpointLocation.Western);
bool gate2Open = CheckpointManager.IsGate2Open(CheckpointLocation.Western);
```

### Comprehensive Checkpoint Information

Use `CheckpointInfo` to get all checkpoint state at once:

```csharp
// Get detailed info for one checkpoint
CheckpointInfo info = CheckpointManager.GetCheckpointInfo(CheckpointLocation.Western);
if (info != null)
{
    Debug.Log($"Checkpoint at {info.Location}:");
    Debug.Log($"  Enabled: {info.IsEnabled}");
    Debug.Log($"  Position: {info.Position}");
    Debug.Log($"  Officers: {info.AssignedOfficerCount}");
    Debug.Log($"  Gate 1: {(info.IsGate1Open ? "Open" : "Closed")}");
    Debug.Log($"  Gate 2: {(info.IsGate2Open ? "Open" : "Closed")}");
    Debug.Log($"  Operational: {info.IsOperational}");
}

// Get info for all checkpoints
List<CheckpointInfo> allCheckpoints = CheckpointManager.GetAllCheckpointInfo();
foreach (var checkpoint in allCheckpoints)
{
    Debug.Log($"{checkpoint.Location}: {(checkpoint.IsOperational ? "Operational" : "Inactive")}");
}
```

### CheckpointInfo Properties

- `Location` - The checkpoint's location enum
- `IsEnabled` - Whether checkpoint is enabled
- `Position` - World position
- `AssignedOfficerCount` - Number of officers currently assigned
- `IsGate1Open` / `IsGate2Open` - Gate status
- `AreBothGatesClosed` - Convenience property for both gates closed
- `IsAnyGateOpen` - Convenience property for any gate open
- `IsOperational` - Enabled AND has at least one officer

## Best Practices

### 1. Check State Before Acting

```csharp
// Check current state before making changes
if (CheckpointManager.IsCheckpointEnabled(CheckpointLocation.Western))
{
    // Only disable if currently enabled
    CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Western, false);
}
```

### 2. Use CheckpointInfo for Decisions

```csharp
// Make informed decisions based on full state
var info = CheckpointManager.GetCheckpointInfo(CheckpointLocation.Western);
if (info != null && !info.IsOperational)
{
    // Checkpoint is enabled but has no officers - request more
    CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Western, true, 2);
}
```

### 3. Consider Officer Availability

```csharp
// Check active officer count before enabling checkpoints
if (LawManager.ActiveOfficerCount < 8)
{
    LoggerInstance.Warning("Not enough officers available for all checkpoints");
    // Enable fewer checkpoints or request fewer officers per checkpoint
    CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Western, true, 1);
}
else
{
    CheckpointManager.EnableAllCheckpoints(2);
}
```

## Example: Checkpoint Monitor

```csharp
using MelonLoader;
using S1API.Law;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CheckpointMonitor : MelonMod
{
    private float _lastCheckTime = 0f;
    private const float CHECK_INTERVAL = 5f;

    public override void OnUpdate()
    {
        if (Time.time - _lastCheckTime > CHECK_INTERVAL)
        {
            _lastCheckTime = Time.time;
            MonitorCheckpoints();
        }
    }

    private void MonitorCheckpoints()
    {
        List<CheckpointInfo> checkpoints = CheckpointManager.GetAllCheckpointInfo();
        int operationalCount = 0;

        StringBuilder report = new StringBuilder();
        report.AppendLine("=== Checkpoint Status ===");
        report.AppendLine($"Law Enforcement Intensity: {LawController.Intensity}/10");
        report.AppendLine();

        foreach (var checkpoint in checkpoints)
        {
            if (checkpoint.IsOperational)
                operationalCount++;

            report.AppendLine($"{checkpoint.Location}:");
            report.AppendLine($"  Status: {(checkpoint.IsOperational ? "OPERATIONAL" : "Inactive")}");
            report.AppendLine($"  Officers: {checkpoint.AssignedOfficerCount}");
            report.AppendLine($"  Position: {checkpoint.Position}");
        }

        report.AppendLine();
        report.AppendLine($"Total Operational: {operationalCount}/4");
        report.AppendLine($"Active Officers: {LawManager.ActiveOfficerCount}");

        LoggerInstance.Msg(report.ToString());
    }
}
```

## Troubleshooting

### Checkpoints Keep Re-enabling

**Problem**: Checkpoints re-enable after being disabled.

**Solution**: The automatic evaluation system is working as designed. Set the law enforcement intensity to 1-4:

```csharp
LawController.SetIntensityLevel(1);
```

### Not Enough Officers at Checkpoints

**Problem**: Checkpoints enable but have no officers.

**Solution**: Check the active officer count and adjust:

```csharp
int available = LawManager.ActiveOfficerCount;
int checkpointsToEnable = available / 2; // 2 officers per checkpoint

// Enable only as many as we can staff
if (checkpointsToEnable >= 1)
{
    CheckpointManager.SetCheckpointEnabled(CheckpointLocation.Western, true, 2);
}
```

### Checkpoints Won't Enable

**Problem**: Checkpoints don't enable when requested.

**Possible causes**:
1. Law enforcement intensity is too low (< 5)
2. No officers available in police station pool
3. Player is too close to checkpoint location (< 50 units)
4. Outside configured time window

**Solution**:
```csharp
// Ensure high intensity
LawController.SetIntensityLevel(7);

// Check officer availability
if (LawManager.ActiveOfficerCount < 2)
{
    LoggerInstance.Warning("No officers available");
}

// Check if checkpoint is actually enabled
var info = CheckpointManager.GetCheckpointInfo(CheckpointLocation.Western);
if (info != null)
{
    LoggerInstance.Msg($"Checkpoint enabled: {info.IsEnabled}, Officers: {info.AssignedOfficerCount}");
}
```


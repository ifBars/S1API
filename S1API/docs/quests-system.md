# Quests System

S1API provides a quest system that allows you to create custom quests with objectives, progress tracking, POI markers, and integration with the game's quest UI.

## Overview

The Quest system allows you to:
- Create custom quests with multiple objectives
- Track quest progress and state
- Display quest markers on the map
- Integrate with the game's quest overlay UI
- Persist quest data with save/load system
- Trigger events on quest state changes

## Creating a Quest

To create a quest, inherit from the `Quest` base class and configure its properties:

```csharp
using S1API.Quests;
using UnityEngine;

public class MyFirstQuest : Quest
{
    protected override string Title => "My First Quest";
    protected override string Description => "Complete this quest to learn the basics.";
    protected override bool AutoBegin => true;

    protected override void OnCreated()
    {
        base.OnCreated();

        // Add quest objectives
        AddEntry("Find the secret location");
        AddEntry("Talk to the mysterious NPC");
        AddEntry("Return to base");
    }
}
```

## Quest Configuration

### Required Properties

```csharp
public class MyQuest : Quest
{
    // Quest title shown in UI
    protected override string Title => "Quest Title";

    // Quest description/summary
    protected override string Description => "Quest description text.";

    // Auto-start the quest when created
    protected override bool AutoBegin => true;

    // Optional: Custom quest icon
    protected override Sprite? QuestIcon => LoadCustomIcon();
}
```

### AutoBegin Behavior

- `AutoBegin = true`: Quest automatically starts when created
- `AutoBegin = false`: Quest must be manually started with `Begin()`

## Quest Entries (Objectives)

Quest entries represent individual objectives that players must complete:

### Adding Entries

```csharp
protected override void OnCreated()
{
    base.OnCreated();

    // Simple text entry
    AddEntry("Find the hideout");

    // Entry with POI marker
    AddEntry(
        text: "Meet at the docks",
        poi: new Vector3(100f, 0f, 50f),  // Map marker position
        poiObjectName: "Docks Meeting Point"
    );

    // Entry with custom icon
    AddEntry(
        text: "Collect the package",
        poi: new Vector3(50f, 0f, 100f),
        poiIcon: customIconSprite
    );
}
```

### Completing Entries

```csharp
// Complete a specific entry by index
CompleteEntry(0);  // Complete first entry

// Complete all entries
CompleteAllEntries();

// Quest automatically completes when all entries are done
// (if you don't override this behavior)
```

### Entry Management

```csharp
// Access quest entries
foreach (var entry in QuestEntries)
{
    bool isComplete = entry.IsCompleted;
    string text = entry.Text;
    Vector3? position = entry.POIPosition;
}

// Clear all entries
QuestEntries.Clear();

// Add new entry dynamically
AddEntry("New objective discovered!");
```

## Quest States

Quests can be in different states:

```csharp
public enum QuestState
{
    Active,      // Quest is in progress
    Complete,    // Quest completed successfully
    Failed,      // Quest failed
    Expired,     // Quest expired (time limit)
    Cancelled    // Quest cancelled
}
```

### Changing Quest State

```csharp
// Complete the quest
Complete();

// Fail the quest
Fail();

// Cancel the quest
Cancel();

// Expire the quest
Expire();

// Check current state
if (State == QuestState.Complete)
{
    MelonLogger.Msg("Quest completed!");
}
```

## Quest Events

Override lifecycle methods to respond to quest events:

```csharp
public class MyQuest : Quest
{
    protected override void OnStarted()
    {
        base.OnStarted();
        MelonLogger.Msg("Quest started!");
    }

    protected override void OnCompleted()
    {
        base.OnCompleted();
        MelonLogger.Msg("Quest completed!");
        // Give rewards, trigger events, etc.
    }

    protected override void OnFailed()
    {
        base.OnFailed();
        MelonLogger.Msg("Quest failed!");
    }

    protected override void OnCancelled()
    {
        base.OnCancelled();
        MelonLogger.Msg("Quest cancelled!");
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        MelonLogger.Msg("Quest loaded from save!");
    }

    protected override void OnSaved()
    {
        base.OnSaved();
        MelonLogger.Msg("Quest saved!");
    }
}
```

## POI Markers

Point of Interest (POI) markers show up on the game map to guide players:

```csharp
protected override void OnCreated()
{
    base.OnCreated();

    // Quest entry with POI marker
    AddEntry(
        text: "Go to North Apartments",
        poi: new Vector3(-28f, 1.065f, 62f),  // World position
        poiObjectName: "North Apartments",     // Optional name
        poiIcon: customSprite                  // Optional custom icon
    );
}
```

## Saving Quest Data

Use the `SaveableField` attribute to persist custom quest data:

```csharp
using S1API.Quests;
using S1API.Saveables;
using System;

public class MyQuest : Quest
{
    [Serializable]
    private class QuestData
    {
        public int KillCount = 0;
        public bool HasFoundSecret = false;
        public List<string> CollectedItems = new List<string>();
    }

    [SaveableField("my_quest_data")]
    private QuestData _data = new QuestData();

    protected override string Title => "Collection Quest";
    protected override string Description =>
        $"Collected: {_data.CollectedItems.Count}/5 items";

    protected override void OnCreated()
    {
        base.OnCreated();
        UpdateQuestDisplay();
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        // Restore quest state from loaded data
        UpdateQuestDisplay();
    }

    public void CollectItem(string itemId)
    {
        if (!_data.CollectedItems.Contains(itemId))
        {
            _data.CollectedItems.Add(itemId);
            UpdateQuestDisplay();

            if (_data.CollectedItems.Count >= 5)
            {
                Complete();
            }

            // Request save
            Saveable.RequestGameSave();
        }
    }

    private void UpdateQuestDisplay()
    {
        QuestEntries.Clear();
        AddEntry($"Items collected: {_data.CollectedItems.Count}/5");
    }
}
```

## Complete Quest Example

Here's a comprehensive quest example with multiple features:

```csharp
using S1API.Quests;
using S1API.Saveables;
using System;
using UnityEngine;

public class DeliveryQuest : Quest
{
    [Serializable]
    private class DeliveryData
    {
        public bool PackagePickedUp = false;
        public bool PackageDelivered = false;
        public int DeliveryTime = 0;
    }

    [SaveableField("delivery_quest_data")]
    private DeliveryData _data = new DeliveryData();

    protected override string Title => "Special Delivery";

    protected override string Description =>
        "Pick up a package and deliver it to the docks.";

    protected override bool AutoBegin => true;

    private Vector3 _pickupLocation = new Vector3(-28f, 1.065f, 62f);
    private Vector3 _deliveryLocation = new Vector3(100f, 0f, 50f);

    protected override void OnCreated()
    {
        base.OnCreated();
        SetupQuestObjectives();
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        SetupQuestObjectives();
    }

    private void SetupQuestObjectives()
    {
        QuestEntries.Clear();

        if (!_data.PackagePickedUp)
        {
            AddEntry(
                "Pick up the package at North Apartments",
                poi: _pickupLocation,
                poiObjectName: "Pickup Location"
            );
        }
        else if (!_data.PackageDelivered)
        {
            AddEntry(
                "Deliver the package to the docks",
                poi: _deliveryLocation,
                poiObjectName: "Delivery Location"
            );
        }
        else
        {
            AddEntry("Package delivered successfully!");
        }
    }

    public void OnPackagePickedUp()
    {
        if (!_data.PackagePickedUp)
        {
            _data.PackagePickedUp = true;
            _data.DeliveryTime = GetCurrentGameTime();

            CompleteEntry(0);
            SetupQuestObjectives();

            Saveable.RequestGameSave();
        }
    }

    public void OnPackageDelivered()
    {
        if (_data.PackagePickedUp && !_data.PackageDelivered)
        {
            _data.PackageDelivered = true;

            CompleteEntry(0);
            Complete();

            Saveable.RequestGameSave();
        }
    }

    protected override void OnCompleted()
    {
        base.OnCompleted();

        int timeElapsed = GetCurrentGameTime() - _data.DeliveryTime;
        MelonLogger.Msg($"Delivery completed in {timeElapsed} minutes!");

        // Award rewards based on delivery time
        if (timeElapsed < 30)
        {
            MelonLogger.Msg("Fast delivery bonus!");
        }
    }

    private int GetCurrentGameTime()
    {
        // Use S1API.GameTime.TimeManager to get current time
        return 0;  // Placeholder
    }
}
```

## Dynamic Quest Tracking Example

Here's an example quest that tracks progress dynamically:

```csharp
using S1API.Quests;
using S1API.Saveables;
using System;

public class SalesQuest : Quest
{
    [Serializable]
    private class SalesData
    {
        public int TotalSales = 0;
        public int TargetSales = 10;
    }

    [SaveableField("sales_quest_data")]
    private SalesData _data = new SalesData();

    protected override string Title => "Sales Target";

    protected override string Description =>
        $"Make {_data.TargetSales} sales to complete this quest.";

    protected override bool AutoBegin => true;

    protected override void OnCreated()
    {
        base.OnCreated();
        UpdateProgressDisplay();
    }

    protected override void OnLoaded()
    {
        base.OnLoaded();
        UpdateProgressDisplay();
    }

    public void OnSaleMade()
    {
        _data.TotalSales++;
        UpdateProgressDisplay();

        if (_data.TotalSales >= _data.TargetSales)
        {
            Complete();
        }

        Saveable.RequestGameSave();
    }

    private void UpdateProgressDisplay()
    {
        QuestEntries.Clear();

        string progressText = $"Sales: {_data.TotalSales}/{_data.TargetSales}";
        AddEntry(progressText);

        if (_data.TotalSales >= _data.TargetSales)
        {
            CompleteEntry(0);
        }
    }

    protected override void OnCompleted()
    {
        base.OnCompleted();
        MelonLogger.Msg("Sales target reached!");
        // Award rewards
    }
}
```

## Quest Registry

Access and manage quests through `QuestManager`:

```csharp
using S1API.Quests;

// Get quest by name
var quest = QuestManager.GetQuestByName("My Quest");

// Get all quests
var allQuests = QuestManager.GetAllQuests();

// Quests are automatically registered when created
```

## Best Practices

1. **Use SaveableField for Data**: Persist quest progress with `[SaveableField]` attributes

2. **Update on Load**: Override `OnLoaded()` to restore quest UI state

3. **Request Saves**: Call `Saveable.RequestGameSave()` after important changes

4. **Clear Before Rebuild**: Clear `QuestEntries` before rebuilding to avoid duplicates

5. **AutoBegin Carefully**: Only use `AutoBegin = true` for quests that should start immediately

6. **POI Cleanup**: Remember that POI markers are cleared when entries are completed

7. **State Management**: Check quest state before making changes to prevent invalid operations

## See Also

- [S1NotesApp Example](https://github.com/ifBars/S1NotesApp) - Contains `StarredNoteQuest`
- [Save System](save-system.md) - For persisting quest data
- [Custom NPCs](custom-npcs.md) - For quest-giving NPCs
- [Quests API Reference](../api/S1API.Quests.html)

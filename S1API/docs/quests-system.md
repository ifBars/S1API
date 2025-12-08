## Quests System

S1API provides a quest system that allows you to create custom quests with objectives, progress tracking, POI markers, and integration with the game's quest UI.

## Overview

The S1API Quest system empowers mod developers to integrate rich narrative elements and dynamic objectives into their mods. It provides high-level APIs to:
- Define custom quests with distinct objectives and progression logic.
- Accurately track quest progress and manage various quest states.
- Display Point-of-Interest (POI) markers on the in-game map, guiding players to objectives.
- Seamlessly integrate with and update the game's native quest overlay UI.
- Automatically persist all custom quest data with the game's save/load system, leveraging S1API's `Saveable` architecture.
- Trigger custom events or logic in response to significant quest state changes or objective completion.

## Creating a Quest

To create a quest, define a class that inherits from the `Quest` base class. S1API automatically discovers and registers these classes via reflection, integrating them into the game's quest system. You can configure core quest properties and define objectives:

```csharp
using S1API.Quests;
using S1API.Quests.Objectives; // Namespace for advanced objective types, if applicable
using UnityEngine; // Potentially needed for scene interaction in objectives

public class MyFirstQuest : Quest
{
    protected override string Title => "My First Quest";
    protected override string Description => "Complete this quest to learn the basics of quest creation.";
    protected override bool AutoBegin => true; // Set to true to automatically start the quest when conditions are met

    // Store references to objectives if you need to manipulate or complete them programmatically
    private QuestEntry _findLocationObjective;
    private QuestEntry _talkToNpcObjective;

    protected override void OnCreated()
    {
        base.OnCreated();

        // Add quest objectives. AddEntry(string) creates a basic text objective.
        // This method returns an QuestEntry object, allowing for further manipulation.
        // More advanced objectives (e.g., requiring player interaction, item collection, or visiting a POI)
        // can be created using specific QuestEntry implementations or builder methods (e.g., AddLocationObjective).
        _findLocationObjective = AddEntry("Find the secret location");
        _talkToNpcObjective = AddEntry("Talk to the mysterious NPC"); // This might integrate with dialogue system
        AddEntry("Return to base"); // This might be completed by entering a specific building
    }

    // Override OnComplete to define logic that runs when the quest finishes successfully
    protected override void OnComplete()
    {
        Debug.Log($"Quest '{Title}' completed!");
        // Grant rewards, unlock new content, trigger follow-up quests, etc.
    }

    // Override OnFail to define logic if the quest is failed (e.g., time limit expires)
    protected override void OnFail()
    {
        Debug.Log($"Quest '{Title}' failed.");
        // Clean up quest-related elements or penalize the player
    }

    // You can also add public methods to your Quest class to trigger objective completion
    // from other parts of your mod, such as an interaction script or a custom event handler.
    public void PlayerFoundSecretLocation()
    {
        if (_findLocationObjective != null && !_findLocationObjective.IsCompleted)
        {
            _findLocationObjective.Complete();
            Debug.Log($"Objective '{_findLocationObjective.Text}' completed.");
            // Request a game save if this is critical data change, if not automatically handled
            // S1API.Saveables.Saveable.RequestGameSave();
        }
    }
}
```

**Key Considerations:**
*   **Objective Types:** While `AddEntry(string)` creates simple text objectives, the `S1API.Quests` module likely provides or integrates with more complex `QuestEntry` implementations for specific tasks like visiting Point-of-Interest (POI) markers, interacting with NPCs, or collecting items. These advanced objective types would be designed to integrate seamlessly with the game's internal systems (e.g., mapping, dialogue). Consult specific `S1API.Quests.Objectives` documentation for available types.
*   **Progression Logic:** Quest objectives are fundamentally driven by in-game events. You will need to subscribe to relevant S1API events (e.g., player movement, dialogue choices, item usage, interaction triggers) and call the `.Complete()` method on the corresponding `QuestEntry` when its conditions are met.
*   **State Persistence:** Quest progress and completion status for `Quest` subclasses are automatically managed and saved by S1API as part of the game's save data. You generally do not need to manually handle serialization for quest state.

## Quest Configuration

### Defining a Custom Quest

Custom quests are defined by inheriting from the `Quest` base class and overriding specific properties to configure their title, description, and behavior.

```csharp
public class MyQuest : Quest
{
    // Quest title shown in UI
    protected override string Title => "My First Custom Quest";

    // Quest description/summary visible to players
    protected override string Description => "Embark on an exciting journey to explore the new areas and complete a series of challenges.";

    // Determines if the quest automatically begins upon creation.
    // Set to 'true' for immediate start, 'false' for manual activation via Quest.Begin().
    protected override bool AutoBegin => true;

    // Optional: Provide a custom icon for the quest. 
    // This method should load or create a Unity Sprite.
    protected override Sprite? QuestIcon => LoadCustomIcon(); // Example: ResourceManager.LoadSprite("MyQuestIcon");
}
```

### Quest AutoBegin Behavior

- `AutoBegin = true`: The quest automatically transitions to the 'active' state immediately upon being instantiated and registered.
- `AutoBegin = false`: The quest must be manually started by invoking the `Begin()` method on the quest instance. This is useful for quests that require specific triggers or player interaction to start.

### Integration and Registration

Custom quest classes, like `MyQuest` above, are instantiated and registered with the S1API runtime using the `CreateQuest<T>(string? guid = null)` method. This method handles both the creation of an instance of your quest class and its registration with the QuestManager for discovery and activation.

To avoid null-reference issues during early startup, subscribe to `Player.LocalPlayerSpawned` in `OnLateInitializeMelon` and create quests inside the event handler after the player is ready:

```csharp
// Example of how to register MyQuest after the player spawns
public class MyMod : MelonMod
{
    public override void OnLateInitializeMelon()
    {
        S1API.Entities.Player.LocalPlayerSpawned += OnLocalPlayerSpawned;
    }

    private void OnLocalPlayerSpawned(S1API.Entities.Player player)
    {
        // Instantiate and register MyQuest.
        // The GUID is optional; if not provided, a unique GUID will be generated.
        S1API.Quests.QuestManager.CreateQuest<MyQuest>();

        // Alternatively, provide a custom GUID for specific referencing:
        // S1API.Quests.QuestManager.CreateQuest<AnotherQuestType>("my_mod_unique_quest_id");
    }
}
```

The `guid` parameter allows you to assign a stable, unique identifier to your quest, which can be useful for referencing it programmatically or ensuring consistent quest state across game sessions. If left `null`, a GUID will be automatically generated upon creation.

## Quest Entries (Objectives)

Quest entries represent individual objectives that players must complete as part of a custom quest.

### Defining a Custom Quest
To create a custom quest, define a class that inherits from `S1API.Quests.Quest`. You must override `QuestId` for a unique identifier and `QuestName` for the display name. Initial entries are typically defined in the `OnCreated()` lifecycle method.

```csharp
using S1API.Quests;
using UnityEngine;

public class MyFirstQuest : Quest
{
    // Unique identifier for your quest
    public override string QuestId => "MY_FIRST_QUEST";
    // Display name for the quest
    public override string QuestName => "A Simple Delivery Quest";

    protected override void OnCreated()
    {
        // Add initial quest entries when the quest object is first created
        AddEntry("Find the hideout");

        AddEntry(
            text: "Meet at the docks",
            poi: new Vector3(100f, 0f, 50f),  // Map marker position
            poiObjectName: "Docks Meeting Point" // Name shown on map
        );

        // Entry with a custom icon. Ensure customIconSprite is a loaded UnityEngine.Sprite.
        // Example: Sprite customIconSprite = S1API.Assets.AssetLoader.LoadSprite("Assets/Icons/package_icon.png");
        Sprite customIconSprite = null; // Placeholder: Replace with actual sprite loading logic
        AddEntry(
            text: "Collect the package",
            poi: new Vector3(50f, 0f, 100f),
            poiIcon: customIconSprite
        );
    }

    protected override void OnComplete()
    {
        // Logic to execute when the quest is fully completed
    }
}
```

### Adding Quest Entries Dynamically
While initial entries are often added in `OnCreated()`, new entries can be added dynamically at any point during the quest's lifecycle.

```csharp
// Add a simple text entry
AddEntry("Investigate the strange noise");

// Add an entry with a Point-of-Interest (POI) marker
AddEntry(
    text: "Report back to HQ",
    poi: new Vector3(25f, 0f, 75f),
    poiObjectName: "Headquarters"
);

// Add an entry with a custom icon and POI
// (Ensure 'customIconSprite' is a loaded UnityEngine.Sprite instance)
AddEntry(
    text: "Find the lost artifact",
    poi: new Vector3(-10f, 0f, -20f),
    poiIcon: customIconSprite
);
```

**`AddEntry` Parameters Explained:**
- `text`: A `string` that describes the current objective to the player. This is displayed in the quest log and HUD.
- `poi`: (Optional) A `UnityEngine.Vector3` indicating the world coordinates where the POI marker should appear on the game map. If omitted, no map marker will be shown for this objective.
- `poiObjectName`: (Optional) A `string` label that is displayed alongside the POI marker on the map, providing additional context (e.g., "North Apartments").
- `poiIcon`: (Optional) A `UnityEngine.Sprite` object. If provided, this sprite will be used as the custom icon for the POI on the map. If not provided, a default quest marker icon will be used by the system.

### Completing Quest Entries
Entries can be marked as complete by their index, or all remaining entries can be completed at once. By default, a quest will automatically complete once all its entries are finished.

```csharp
// Complete a specific entry by its zero-based index
CompleteEntry(0);  // Marks the first entry as completed

// Complete all remaining entries for this quest
CompleteAllEntries();

// The quest automatically transitions to 'Completed' state
// once all its entries are marked as done, unless overridden.
```

### Entry Management
Access and manage quest entries dynamically using the `QuestEntries` property, which returns a collection of `QuestEntry` objects.

```csharp
// Iterate through all current quest entries
foreach (var entry in QuestEntries)
{
    bool isComplete = entry.IsCompleted;
    string text = entry.Text;
    Vector3? position = entry.POIPosition; // Null if no POI is set
    Sprite icon = entry.POIIcon;          // Null if no custom icon is set
    // ... other properties like entry.POIObject
}

// Clear all entries from the quest. Use with caution, as this removes all objectives.
QuestEntries.Clear();

// Add a new entry dynamically during quest runtime (e.g., based on player action)
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

Override lifecycle methods to respond to quest events. For a quest's custom state to persist across game saves, ensure relevant fields are marked with `[SaveableField("key")]` if your quest class inherits from `S1API.Internal.Abstraction.Saveable`, or if the `Quest` base class handles `Saveable` integration internally.

```csharp
using S1API.Internal.Abstraction; // For [SaveableField]
using S1API.Quests; // Assuming Quest is here
using MelonLoader; // For MelonLogger.Msg

public class MyPersistentQuest : Quest
{
    // Example of a field that needs to persist with the quest
    [SaveableField("myQuestProgress")]
    private int currentObjectiveIndex = 0;

    protected override void OnCreated()
    {
        MelonLogger.Msg("MyPersistentQuest created! Current Objective: " + currentObjectiveIndex);
        // Example: Display initial quest objective in UI or trigger an introductory dialogue.
    }

    protected override void OnComplete()
    {
        MelonLogger.Msg("MyPersistentQuest completed! Awarding player...");
        // Example: Award items, money, or unlock new content.
        // PlayerInventory.AddItem(new S1API.Items.ExampleItem());
        
        // If completing the quest changes critical game state that needs saving immediately,
        // you can request a game save.
        // Saveable.RequestGameSave();
    }

    protected override void OnFail()
    {
        MelonLogger.Msg("MyPersistentQuest failed. Resetting state or penalizing player.");
        // Example: Revert changes, apply penalties, or restart the quest.
    }

    protected override void OnLoaded()
    {
        MelonLogger.Msg("MyPersistentQuest loaded from save! Resuming at objective: " + currentObjectiveIndex);
        // Example: Apply the loaded 'currentObjectiveIndex' to the game world (e.g., set the active objective in UI).
    }
    
    // Note: To save critical changes to your quest's state, call `RequestGameSave()`
    // when a significant event occurs (e.g., objective completion, progress update).
    // There is no `OnSaved()` override for individual `Saveable` components; saving is managed by the system.
}
```

## POI Markers

Point of Interest (POI) markers are essential for guiding players to objectives on the game map within your custom quests. To utilize them, you'll define a new quest class by inheriting from `S1API.Quests.Quest` and add your objectives and POI markers within its `OnCreated()` method.

First, define your custom quest class:

```csharp
using UnityEngine; // For Vector3 and Sprite
using MelonLoader; // For MelonMod
using S1API.Quests;

public class GoToNorthApartmentsQuest : Quest
{
    // A unique ID for your quest. This is essential for the QuestSystem to identify, save, and load quest progress.
    public GoToNorthApartmentsQuest() : base("go_to_north_apartments_quest_id") { }

    // The OnCreated method is called once when the quest is first instantiated.
    // Use this to define your initial objectives and POI markers.
    protected override void OnCreated()
    {
        // Add an entry (objective) to the quest, optionally with a POI marker.
        // Players will see the 'text' as their current goal, and the POI will guide them.
        AddEntry(
            text: "Go to North Apartments", // The text displayed for this objective.
            poi: new Vector3(-28f, 1.065f, 62f),  // World position for the POI marker.
            poiObjectName: "North Apartments",     // Optional name displayed on the map next to the POI.
            poiIcon: customSprite                  // Optional custom icon (UnityEngine.Sprite) for the POI.
        );

        // You can add additional entries/objectives here if your quest has multiple steps.
        // AddEntry("Find the hidden key in the apartments");
    }

    // Optional: Override other lifecycle methods like OnStarted(), OnComplete(), or OnFailed()
    // to implement quest-specific logic at different stages.
    // protected override void OnComplete() { /* Grant rewards, unlock new content, etc. */ }
}
```

## Saving Quest Data

Custom quests, inheriting from `S1API.Quests.Quest`, automatically gain access to the S1API persistence system because `Quest` itself inherits from `S1API.Internal.Abstraction.Saveable`. This allows you to easily persist custom quest data using the `SaveableField` attribute on private fields within your quest class.

Here's an example demonstrating how to save and load quest-specific progress, such as collected items:

```csharp
using S1API.Quests;
using S1API.Saveables;
using System;
using System.Collections.Generic;

public class MyQuest : Quest
{
    // Define a serializable data structure for your quest's state
    [Serializable]
    private class QuestData
    {
        public int KillCount = 0;
        public bool HasFoundSecret = false;
        public List<string> CollectedItems = new List<string>();
    }

    // Use SaveableField to persist an instance of your QuestData class
    // The string "my_quest_data" acts as a unique key for this field in the save file.
    [SaveableField("my_quest_data")]
    private QuestData _data = new QuestData();

    // Define quest title and description, potentially dynamic based on saved data
    protected override string Title => "Collection Quest";
    protected override string Description =>
        $"Collected: {_data.CollectedItems.Count}/5 items";

    // OnCreated is called when the quest is first initialized
    protected override void OnCreated()
    {
        UpdateQuestDisplay();
    }

    // OnLoaded is called when the quest data is deserialized from a save file
    protected override void OnLoaded()
    {
        // Restore quest state from loaded data and update UI/game elements
        UpdateQuestDisplay();
    }

    // Example method to modify quest data and request a game save
    public void CollectItem(string itemId)
    {
        if (!_data.CollectedItems.Contains(itemId))
        {
            _data.CollectedItems.Add(itemId);
            UpdateQuestDisplay();

            if (_data.CollectedItems.Count >= 5)
            {
                Complete(); // Mark quest as complete
            }

            // Request a game save whenever critical quest data changes
            Saveable.RequestGameSave();
        }
    }

    // Helper to update the quest's display entries
    private void UpdateQuestDisplay()
    {
        QuestEntries.Clear(); // Clear existing entries
        AddEntry($"Items collected: {_data.CollectedItems.Count}/5");
    }
}
```

## Quest Access

Access and manage quests through `QuestManager` after defining your custom quest types.

## Defining a Custom Quest
1.  **Class Definition**: Create a class inheriting from `S1API.Quests.Quest`.
2.  **Registration**: Custom quest classes are instantiated and registered with the S1API runtime using the `CreateQuest<T>(string? guid = null)` method. This method handles both the creation of an instance of your quest class and its registration with the QuestManager for discovery and activation.

## Accessing Registered Quests
Once defined and registered, quests can be retrieved from the `QuestManager`:

```csharp
using S1API.Quests;

// Get quest by unique ID
var questById = QuestManager.GetQuestByGuid("MY_QUEST_001");

// Get quest by DisplayName
var questByName = QuestManager.GetQuestByName("The Grand Adventure");

// Get a quest by type (useful for base game quests)
var allQuests = QuestManager.Get<CleanCash>();
```

## Best Practices

1. **Use SaveableField for Data**: Persist quest progress with `[SaveableField]` attributes

2. **Update on Load**: Override `OnLoaded()` to restore quest UI state

3. **Request Saves**: Call `RequestGameSave()` after important changes

4. **Clear Before Rebuild**: Clear `QuestEntries` before rebuilding to avoid duplicates

5. **AutoBegin Carefully**: Only use `AutoBegin = true` for quests that should start immediately

6. **State Management**: Check quest state before making changes to prevent invalid operations

## See Also

- [S1NotesApp Example](https://github.com/ifBars/S1NotesApp) - Uses `StarredNoteQuest` with `NotesManager` and `NotesApp`
- [Save System](save-system.md) - For persisting quest data
- [Custom NPCs](custom-npcs.md) - For quest-giving NPCs
- <xref:S1API.Quests> - Quests API Reference
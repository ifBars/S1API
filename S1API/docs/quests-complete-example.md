## Complete Example: Multi-Phase Quest

This guide demonstrates a complete multi-phase heist quest with NPC-triggered activation, parallel objectives, sequential phases, and state persistence.

## Overview

"The Big Score" is a quest where the player must:
1. Talk to an informant NPC to receive the job
2. Recruit three crew members (can be done in any order)
3. Execute the heist once the crew is assembled
4. Report back to the informant

## Quest Class

```csharp
using S1API.Quests;
using S1API.Saveables;
using UnityEngine;

/// <summary>
/// "The Big Score" - A multi-part heist quest.
///
/// Story:
/// An NPC tells you about a rival dealer's stash house.
/// To pull off the heist, you need to recruit a crew:
/// - Zero (Hacker) - Disables security
/// - Tank (Muscle) - Protection
/// - Roxy (Inside Contact) - Handles the cops
/// </summary>
public class TheBigScoreQuest : Quest
{
    protected override string Title => "The Big Score";
    protected override string Description => "Assemble a crew and pull off the heist.";
    protected override bool AutoBegin => false; // Started by talking to NPC

    // NPC spawn positions for map markers
    public static readonly Vector3 QuestGiverPos = new Vector3(-118.9f, -2.935f, 88.6f);
    public static readonly Vector3 MusclePos = new Vector3(-137.2f, -2.346f, 99.4f);
    public static readonly Vector3 ContactPos = new Vector3(58.8f, 1.065f, 43.1f);
    public static readonly Vector3 HeistPos = new Vector3(-96.92f, -1.47f, -36.96f);

    // Track quest progress with SaveableField for persistence
    [SaveableField("HackerRecruited")]
    private bool _hackerRecruited = false;

    [SaveableField("MuscleRecruited")]
    private bool _muscleRecruited = false;

    [SaveableField("ContactRecruited")]
    private bool _contactRecruited = false;

    [SaveableField("HeistComplete")]
    private bool _heistComplete = false;

    // Quest entries (objectives) - store references for later control
    private QuestEntry? _entryTalkToNPC;
    private QuestEntry? _entryRecruitHacker;
    private QuestEntry? _entryRecruitMuscle;
    private QuestEntry? _entryRecruitContact;
    private QuestEntry? _entryExecuteHeist;
    private QuestEntry? _entryReportBack;

    public TheBigScoreQuest()
    {
        SetupQuestEntries();
    }

    private void SetupQuestEntries()
    {
        // Phase 1: Get the tip from the quest giver
        _entryTalkToNPC = AddEntry(
            "Talk to the informant",
            QuestGiverPos  // Shows POI marker on map
        );

        // Phase 2: Recruit the crew (can be done in any order)
        // Note: These don't Begin() automatically - we call Begin() after Phase 1
        _entryRecruitHacker = AddEntry("Recruit Zero (Hacker) - $500 via text");

        _entryRecruitMuscle = AddEntry(
            "Recruit Tank (Muscle) - $1,000",
            MusclePos
        );

        _entryRecruitContact = AddEntry(
            "Recruit Roxy (Inside Contact) - $500",
            ContactPos
        );

        // Phase 3: Execute the heist
        _entryExecuteHeist = AddEntry(
            "Hit the stash at the docks",
            HeistPos
        );

        // Phase 4: Report back
        _entryReportBack = AddEntry(
            "Report back to the informant",
            QuestGiverPos
        );
    }

    /// <summary>
    /// Start the quest (called when player talks to quest-giving NPC)
    /// </summary>
    public void StartQuest()
    {
        Begin();  // Activates the quest
    }

    /// <summary>
    /// Called when player completes initial conversation
    /// </summary>
    public void CompleteIntroDialogue()
    {
        if (_entryTalkToNPC == null) return;

        _entryTalkToNPC.Complete();

        // Begin the recruitment objectives so they appear in UI
        // All three can be done in parallel
        _entryRecruitHacker?.Begin();
        _entryRecruitMuscle?.Begin();
        _entryRecruitContact?.Begin();
    }

    /// <summary>
    /// Mark the hacker as recruited
    /// </summary>
    public void RecruitHacker()
    {
        if (_hackerRecruited) return;

        _hackerRecruited = true;
        _entryRecruitHacker?.Complete();
        CheckCrewComplete();
    }

    /// <summary>
    /// Mark the muscle as recruited
    /// </summary>
    public void RecruitMuscle()
    {
        if (_muscleRecruited) return;

        _muscleRecruited = true;
        _entryRecruitMuscle?.Complete();
        CheckCrewComplete();
    }

    /// <summary>
    /// Mark the inside contact as recruited
    /// </summary>
    public void RecruitContact()
    {
        if (_contactRecruited) return;

        _contactRecruited = true;
        _entryRecruitContact?.Complete();
        CheckCrewComplete();
    }

    /// <summary>
    /// Check if all crew members are recruited, advance to next phase
    /// </summary>
    private void CheckCrewComplete()
    {
        if (IsCrewComplete)
        {
            // Begin the heist objective so it appears in UI
            _entryExecuteHeist?.Begin();
        }
    }

    /// <summary>
    /// Check if crew is fully assembled
    /// </summary>
    public bool IsCrewComplete => _hackerRecruited && _muscleRecruited && _contactRecruited;

    /// <summary>
    /// Check if heist has been completed
    /// </summary>
    public bool IsHeistComplete => _heistComplete;

    /// <summary>
    /// Execute the heist
    /// </summary>
    public void ExecuteHeist()
    {
        if (_heistComplete) return;
        if (!IsCrewComplete) return;

        _heistComplete = true;
        _entryExecuteHeist?.Complete();

        // Begin the report back objective
        _entryReportBack?.Begin();
    }

    /// <summary>
    /// Complete the quest
    /// </summary>
    public void ReportBack()
    {
        if (!_heistComplete) return;

        _entryReportBack?.Complete();
        Complete();
    }
}
```

## Quest Manager

Use a static manager class to coordinate quest state with NPCs and game systems:

```csharp
using S1API.Quests;
using MelonLoader;

public static class BigScoreManager
{
    private static TheBigScoreQuest? _quest;
    private static bool _questStarted = false;

    public static TheBigScoreQuest? Quest => _quest;
    public static bool QuestStarted => _questStarted;
    public static bool IsCrewComplete => _quest?.IsCrewComplete ?? false;

    public static void Initialize()
    {
        // Create the quest via QuestManager
        _quest = (TheBigScoreQuest)QuestManager.CreateQuest<TheBigScoreQuest>();
    }

    public static void StartQuest()
    {
        if (_questStarted || _quest == null) return;
        _quest.StartQuest();
        _questStarted = true;
    }

    public static void OnIntroComplete()
    {
        _quest?.CompleteIntroDialogue();
    }

    public static void RecruitHacker() => _quest?.RecruitHacker();
    public static void RecruitMuscle() => _quest?.RecruitMuscle();
    public static void RecruitContact() => _quest?.RecruitContact();

    public static void ExecuteHeist() => _quest?.ExecuteHeist();
    public static void ReportBack() => _quest?.ReportBack();
}
```

## Integration with NPCs

Trigger quest progression from NPC dialogue callbacks:

```csharp
using S1API.Entities;

public sealed class QuestGiverNPC : NPC
{
    protected override void OnCreated()
    {
        // Set up dialogue for quest initiation
        Dialogue.BuildAndRegisterContainer("QuestIntro", container =>
        {
            container
                .AddNode("ENTRY", "I've got a job for you...", choices => choices
                    .Add("accept", "I'm in.", "details")
                    .Add("decline", "Not interested.", "goodbye"))
                .AddNode("details", "You'll need a crew. Find a hacker, muscle, and an inside contact.", choices => choices
                    .Add("confirm", "Got it.", "end"))
                .AddNode("goodbye", "Your loss.")
                .AddNode("end", "Good luck out there.");
        });

        // Handle when player accepts the quest
        Dialogue.OnChoiceSelected("accept", () =>
        {
            if (!BigScoreManager.QuestStarted)
            {
                BigScoreManager.StartQuest();
            }
            BigScoreManager.OnIntroComplete();
        });

        Dialogue.UseContainerOnInteract("QuestIntro");
    }
}
```

## Mod Entry Point

Initialize the quest system when the game scene loads:

```csharp
using MelonLoader;

public class Core : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main")
        {
            // Initialize the quest manager after game loads
            BigScoreManager.Initialize();
        }
    }
}
```

## Key Patterns

### 1. NPC-Triggered Activation

Set `AutoBegin => false` so the quest doesn't start automatically. Call `Begin()` when the player accepts via dialogue:

```csharp
protected override bool AutoBegin => false;

public void StartQuest()
{
    Begin();  // Manually activate the quest
}
```

### 2. Phased Objectives

Use `Begin()` on individual entries to control when they appear in the UI. Entries added with `AddEntry()` don't show until `Begin()` is called:

```csharp
// After completing Phase 1, begin Phase 2 objectives
_entryTalkToNPC.Complete();
_entryRecruitHacker?.Begin();
_entryRecruitMuscle?.Begin();
_entryRecruitContact?.Begin();
```

### 3. Parallel Objectives

Multiple objectives can be active simultaneously. Players can complete them in any order:

```csharp
// All three recruitment objectives are active at once
_entryRecruitHacker?.Begin();
_entryRecruitMuscle?.Begin();
_entryRecruitContact?.Begin();
```

### 4. Sequential Phases with Prerequisites

Check prerequisites before advancing to the next phase:

```csharp
public bool IsCrewComplete => _hackerRecruited && _muscleRecruited && _contactRecruited;

private void CheckCrewComplete()
{
    if (IsCrewComplete)
    {
        _entryExecuteHeist?.Begin();  // Only show when ready
    }
}
```

### 5. Persistent State

Use `[SaveableField]` to persist progress across save/load:

```csharp
[SaveableField("HackerRecruited")]
private bool _hackerRecruited = false;

[SaveableField("MuscleRecruited")]
private bool _muscleRecruited = false;
```

### 6. Map Markers (POI)

Pass a `Vector3` position to `AddEntry()` to show a marker on the map:

```csharp
_entryRecruitMuscle = AddEntry(
    "Recruit Tank (Muscle) - $1,000",
    MusclePos  // Shows POI marker at this position
);
```

### 7. External Control

Expose public methods so NPCs, managers, and game systems can control quest progression:

```csharp
public void RecruitHacker() { /* ... */ }
public void RecruitMuscle() { /* ... */ }
public void ExecuteHeist() { /* ... */ }
```

## See Also

- [Quests System](quests-system.md) - Core quest API documentation
- [Custom NPCs](custom-npcs.md) - Creating quest-giving NPCs
- [Dialogue System](dialogue-system.md) - NPC dialogue integration
- [Save System](save-system.md) - Persisting quest data

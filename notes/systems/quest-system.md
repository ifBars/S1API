---
title: "Quest System"
description: "Reference for the quest system — Quest, QuestEntry, EQuestState, state machine, and HUD tracking"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Quest System

## Quest (`Il2CppScheduleOne.Quests.Quest`)
MonoBehaviour, IGUIDRegisterable, ISaveable. Base class for all quests.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `MAX_HUD_ENTRY_LABELS` | 10 | Max HUD entries shown |
| `CriticalExpiryThreshold` | 120 | Minutes before expiry warning |

### Static Members
| Member | Type | Description |
|--------|------|-------------|
| `Quests` | `List<Quest>` | All quests in scene |
| `HoveredQuest` | `Quest` | Currently hovered in UI |
| `ActiveQuests` | `List<Quest>` | Currently active quests |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Quest display title |
| `Subtitle` | `string` | Quest subtitle |
| `Description` | `string` | Full description |
| `StaticGUID` | `string` | Unique quest identifier |
| `TrackOnBegin` | `bool` | Auto-track on start |
| `AutoCompleteOnAllEntriesComplete` | `bool` | Auto-complete flag |
| `CompletionXP` | `int` | XP awarded on completion |
| `AutoStartFirstEntry` | `bool` | Auto-start entry 0 |
| `Entries` | `List<QuestEntry>` | Quest entry list |
| `ExpiryVisibility` | `EExpiryVisibility` | How expiry is shown |
| `PlayQuestCompleteSound` | `bool` | Sound on complete |

### Events
| Event | Args | Description |
|-------|------|-------------|
| `onQuestBegin` | `UnityEvent` | Quest started |
| `onQuestEnd` | `UnityEvent<EQuestState>` | Quest ended (any result) |
| `onActiveState` | `UnityEvent` | Quest became active |
| `onTrackChange` | `UnityEvent<bool>` | Tracking toggled |
| `onComplete` | `UnityEvent` | Quest completed |

### Methods
| Method | Description |
|--------|-------------|
| `Begin(bool network)` | Start the quest |
| `Success()` | Complete quest successfully |
| `Fail()` | Mark quest as failed |
| `Expire()` | Quest expired (time limit) |
| `Cancel()` | Cancel quest |
| `SetQuestState(EQuestState)` | Set arbitrary state |
| `IsTracked()` / `SetTracked(bool)` | Track in HUD |

---

## EQuestState Enum
| Value | Index | Description |
|-------|-------|-------------|
| `Inactive` | 0 | Not yet started |
| `Active` | 1 | Currently in progress |
| `Completed` | 2 | Successfully finished |
| `Failed` | 3 | Failed |
| `Expired` | 4 | Time limit expired |
| `Cancelled` | 5 | Cancelled |

---

## QuestManager (`Il2CppScheduleOne.Quests.QuestManager`)
`NetworkSingleton<QuestManager>`, ISaveable. Manages all quest lifecycle.

### EQuestAction Enum
| Value | Index |
|-------|-------|
| `Begin` | 0 |
| `Success` | 1 |
| `Fail` | 2 |
| `Expire` | 3 |
| `Cancel` | 4 |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `DefaultQuests` | `Quest[]` | Quests to auto-spawn |
| `QuestContainer` | `Transform` | Parent for quest GameObjects |
| `ContractContainer` | `Transform` | Parent for contract GameObjects |
| `ContractPrefab` | `Contract` | Contract prefab |
| `DeaddropCollectionPrefab` | `DeaddropQuest` | Dead drop prefab |

### Methods
| Method | Description |
|--------|-------------|
| `CreateQuest(Quest prefab)` | Instantiate + track quest |
| `CreateContract(...)` | Create dealer contract |
| `GetQuest(string guid)` | Find quest by GUID |
| `GetActiveQuest(string guid)` | Find active quest by GUID |
| `QuestExists(string guid)` | Check if quest exists |
| `SetQuestState(string guid, EQuestState)` | Set quest state by GUID |

---

## QuestEntry (`Il2CppScheduleOne.Quests.QuestEntry`)
Individual objective within a quest.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Entry description |
| `IsComplete` | `bool` | Completion status |
| `IsOptional` | `bool` | Optional flag |
| `EntryGUID` | `string` | Unique entry ID |

### Methods
| Method | Description |
|--------|-------------|
| `SetCompleted(bool)` | Mark complete/incomplete |
| `SetActive(bool)` | Toggle active state |

---

## StateMachine (`Il2CppScheduleOne.Quests.StateMachine`)
Controls branching quest state transitions.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `CurrentState` | `int` | Current state index |
| `States` | `List<State>` | All possible states |

### Methods
| Method | Description |
|--------|-------------|
| `TransitionTo(int state)` | Change to new state |
| `GetCurrentState()` | Current state data |

---

## QuestWindowConfig (`Il2CppScheduleOne.Quests.QuestWindowConfig`)
Configuration for quest UI window.

---

## SystemTrigger (`Il2CppScheduleOne.Quests.SystemTrigger`)
Quest trigger based on game systems (time, items, regions).

---

## All Game Quests

| Quest Class | GUID / Name | Description |
|-------------|-------------|-------------|
| `Quest_WelcomeToHylandPoint` | `welcome_to_hyland_point` | Tutorial intro |
| `Quest_GettingStarted` | `getting_started` | First steps |
| `Quest_WeNeedToCook` | `we_need_to_cook` | Cooking tutorial |
| `Quest_DownToBusiness` | `down_to_business` | First sales |
| `Quest_NeedingTheGreen` | `needing_the_green` | Grow setup |
| `Quest_OnTheGrind` | `on_the_grind` | Expansion |
| `Quest_GearingUp` | `gearing_up` | Equipment |
| `Quest_Connections` | `connections` | Meet contacts |
| `Quest_SecuringSupplies` | `securing_supplies` | Supplier chain |
| `Quest_ExpandingOperations` | `expanding_operations` | Property expansion |
| `Quest_Warehouse` | `warehouse` | Warehouse unlock |
| `Quest_GrowShrooms` | `grow_shrooms` | Shroom cultivation |
| `Quest_MovingUp` | `moving_up` | Rank progression |
| `Quest_Employees` | `employees` | Hire first employee |
| `Quest_Botanists` | `botanists` | Hire botanist |
| `Quest_Chemists` | `chemists` | Hire chemist |
| `Quest_Cleaners` | `cleaners` | Hire cleaner |
| `Quest_Packagers` | `packagers` | Hire packager |
| `Quest_CleanCash` | `clean_cash` | Money laundering |
| `Quest_UnfavourableAgreements` | `unfavourable_agreements` | Cartel deal |
| `Quest_DealForCartel` | `deal_for_cartel` | Cartel mission |
| `Quest_TheDeepEnd` | `the_deep_end` | Endgame start |
| `Quest_SinkOrSwim` | `sink_or_swim` | Endgame |
| `Quest_DefeatCartel` | `defeat_cartel` | Final quest |
| `Contract` | (dynamic) | Dealer contracts |
| `DeaddropQuest` | (dynamic) | Dead drop deliveries |

---

## Contract System

### Contract (`Il2CppScheduleOne.Quests.Contract`) extends Quest
Dealer-to-customer delivery contracts.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `ProductList` | `ProductList` | Required products |
| `Customer` | `GameObject` | Customer reference |
| `Dealer` | `Dealer` | Assigned dealer |
| `Payment` | `float` | Payment amount |
| `DeliveryWindow` | (time range) | Delivery time window |
| `DeliveryLocation` | `Transform` | Drop-off point |
| `State` | `EQuestState` | Current contract state |
| `hudUI` | `GameObject` | World-space HUD element |

### Methods
| Method | Description |
|--------|-------------|
| `SubmitPayment(float bonus)` | Pay player (bonus added) |
| `Complete(bool replicate)` | Mark contract done |

### ContractInfo (`Il2CppScheduleOne.Quests.ContractInfo`)
Data class holding contract generation info.

---

## Usage Patterns

### Starting a quest
```csharp
Quest quest = QuestManager.CreateQuest(questPrefab);
quest.Begin(true); // network-synced
```

### Checking quest state
```csharp
Quest quest = QuestManager.GetQuest("getting_started");
if (quest != null && quest.State == EQuestState.Active)
{
    // Quest is in progress
}
```

### Listening for quest events
```csharp
Quest quest = QuestManager.GetQuest("warehouse");
quest.onQuestBegin.AddListener(() => MelonLogger.Msg("Warehouse quest started!"));
quest.onQuestEnd.AddListener((state) => MelonLogger.Msg("Warehouse quest ended: " + state));
```

### Creating entry completion quest objectives
```csharp
foreach (QuestEntry entry in quest.Entries)
{
    if (!entry.IsComplete && entry.Text.Contains("grow"))
    {
        entry.SetCompleted(true);
        break;
    }
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Quest.GetActiveQuests()` | Get active quests |
| `Api.Quest.CompleteQuest()` | Complete quest |
| `Api.Quest.TrackQuest()` | Track quest |
| `Api.Quest.GetQuestState()` | Get quest status |

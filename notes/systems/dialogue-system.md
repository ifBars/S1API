---
title: "Dialogue System"
description: "Reference for the dialogue system — DialogueController, DialogueHandler, DialogueContainer, and conversation flow"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Dialogue System

## DialogueController (`Il2CppScheduleOne.Dialogue.DialogueController`)
MonoBehaviour on NPCs. Handles player interaction trigger for dialogue.

### Nested Class: DialogueChoice
| Field/Method | Type | Description |
|-------------|------|-------------|
| `Enabled` | `bool` | Whether choice is available |
| `ChoiceText` | `string` | Display text |
| `ShowWorldspaceDialogue` | `bool` | Show floating text |
| `Conversation` | `DialogueContainer` | Conversation to open |
| `onChoosen` | `UnityEvent` | When choice selected |
| `shouldShowCheck` | `ShouldShowCheck` | Delegate to check visibility |
| `isValidCheck` | `IsChoiceValid` | Delegate to check validity |
| `Priority` | `int` | Display order |
| `ShouldShow()` | `bool` | Evaluates visibility |
| `IsValid(out string)` | `bool` | Evaluates validity |

### Nested Class: GreetingOverride
| Field | Type | Description |
|-------|------|-------------|
| `Greeting` | `string` | Custom greeting |
| `ShouldShow` | `bool` | Show condition |
| `PlayVO` | `bool` | Play voice-over |
| `VOType` | `EVOLineType` | Voice-over type |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `IntObj` | `InteractableObject` | The interactable trigger |
| `OverrideContainer` | `DialogueContainer` | Override for special dialogue (e.g. checkpoint) |

### Methods
| Method | Description |
|--------|-------------|
| `StartConversation(DialogueContainer)` | Opens dialogue with given container |
| `EndConversation()` | Closes current dialogue |
| `SetDialogueChoice(int index, DialogueChoice)` | Modify a choice at runtime |

---

## DialogueHandler (`Il2CppScheduleOne.Dialogue.DialogueHandler`)
MonoBehaviour managing the active dialogue flow.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `TimePerChar` | 0.2s | Time per character for typewriter |
| `WorldspaceDialogueMinDuration` | 1.5s | Min bubble display time |
| `WorldspaceDialogueMaxDuration` | 5s | Max bubble display time |

### Static
| Member | Type | Description |
|--------|------|-------------|
| `activeDialogue` | `DialogueContainer` | Currently open dialogue |
| `activeDialogueNode` | `DialogueNodeData` | Currently displayed node |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `IsDialogueInProgress` | `bool` | Whether dialogue is active |
| `CurrentChoices` | `List<DialogueChoiceData>` | Available player choices |
| `runtimeModules` | `List<DialogueModule>` | Modules added at runtime |
| `NPC` | `NPC` (get) | The NPC this handler belongs to |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Database` | `DialogueDatabase` | All dialogue data |
| `LookPosition` | `Transform` | NPC look target during dialogue |
| `WorldspaceRend` | `WorldspaceDialogueRenderer` | Floating bubble renderer |
| `VOEmitter` | `VOEmitter` | Voice-over emitter |
| `DialogueEvents` | `DialogueEvent[]` | Events triggered during dialogue |
| `onConversationStart` | `UnityEvent` | Conversation started |
| `onDialogueNodeDisplayed` | `UnityEvent<string>` | Node displayed |
| `onDialogueChoiceChosen` | `UnityEvent<string>` | Choice selected |

### Methods
| Method | Description |
|--------|-------------|
| `StartConversation(DialogueContainer)` | Begin dialogue |
| `AdvanceDialogue()` | Move to next node |
| `SelectChoice(int index)` | Player selected a choice |
| `EndConversation()` | Close dialogue |

---

## DialogueContainer (`Il2CppScheduleOne.Dialogue.DialogueContainer`)
ScriptableObject. Holds a full dialogue graph.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| (node link data) | `List<NodeLinkData>` | Node connections |
| (node data) | `List<DialogueNodeData>` | All dialogue nodes |
| (choice data) | `List<DialogueChoiceData>` | Choice nodes |

---

## DialogueDatabase (`Il2CppScheduleOne.Dialogue.DialogueDatabase`)
ScriptableObject. Database of all dialogue for an NPC.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `GenericEntries` | `List<Entry>` | Generic dialogue entries |
| `Modules` | `List<DialogueModule>` | Dialogue modules |

### Methods
| Method | Description |
|--------|-------------|
| `Initialize(DialogueHandler)` | Setup references |

---

## DialogueModule (`Il2CppScheduleOne.Dialogue.DialogueModule`)
Module system for contextual dialogue.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `ModuleType` | `EDialogueModule` | Type identifier |
| `Entries` | `List<Entry>` | Module-specific entries |

### EDialogueModule Enum
| Value | Description |
|-------|-------------|
| `Generic` | Base module (always present) |
| (per-NPC) | NPC-specific modules |
| (dynamic) | Runtime-added modules |

---

## DialogueNodeData / DialogueChoiceData / BranchNodeData / BranchOptionData
Data classes for the dialogue graph structure.

| Class | Purpose |
|-------|---------|
| `DialogueNodeData` | A dialogue line (NPC speaks) |
| `DialogueChoiceData` | A player response choice |
| `BranchNodeData` | Branching condition node |
| `BranchOptionData` | Branch option with condition |

---

## DialogueController Specializations
Pre-built dialogue controllers for key NPCs:

| Controller | NPC | Purpose |
|-----------|-----|---------|
| `DialogueController_Dealer` | Dealers | Product orders, payments |
| `DialogueController_Supplier` | Suppliers | Material ordering |
| `DialogueController_Employee` | Employees | Work management |
| `DialogueController_Police` | Police | Checkpoint, arrest |
| `DialogueController_Fixer` | Fixer | Bribes, dispute resolution |
| `DialogueController_Billy` | Billy | Tutorial/story |
| `DialogueController_Dan` | Dan | Tutorial/story |
| `DialogueController_Jen` | Jen | Tutorial/story |
| `DialogueController_Ming` | Ming | Tutorial/story |
| `DialogueController_Oscar` | Oscar | Tutorial/story |
| `DialogueController_Sam` | Sam | Tutorial/story |
| `DialogueController_ThomasBenzies` | Thomas Benzies | Tutorial/story |
| `DialogueController_ArmsDealer` | Arms Dealer | Weapon purchases |
| `DialogueController_SkateboardSeller` | Skateboard Seller | Vehicle/skateboard |

---

## Dialogue Handlers
| Handler | Purpose |
|---------|---------|
| `DialogueHandler_Customer` | Customer interactions |
| `DialogueHandler_EstateAgent` | Property purchasing |
| `DialogueHandler_Police` | Police interactions |
| `DialogueHandler_VehicleSalesman` | Vehicle purchasing |

---

## DialogueChain / DialogueList / Entry
| Class | Purpose |
|-------|---------|
| `DialogueChain` | Sequential dialogue chain |
| `DialogueList` | List of dialogue entries |
| `Entry` | Single dialogue entry (node) |

---

## DialogueEvent / DialogueNodeEvent
Event hooks triggered during dialogue flow.

---

## VocalReactionDatabase
Maps dialogue to voice-over reactions.

---

## ControlledDialogueHandler
For scripted/cutscene dialogue sequences.

---

## DialogueChoiceEnabler
Runtime logic to enable/disable dialogue choices.

---

## Usage Patterns

### Adding a dynamic dialogue choice
```csharp
DialogueController controller = npc.GetComponent<DialogueController>();
var newChoice = new DialogueController.DialogueChoice
{
    ChoiceText = "Ask about custom quest",
    Conversation = myCustomDialogue,
    Enabled = true,
    Priority = 5
};
controller.SetDialogueChoice(slotIndex, newChoice);
```

### Starting a conversation manually
```csharp
DialogueHandler handler = npc.GetComponent<DialogueHandler>();
handler.StartConversation(myDialogueContainer);
```

### Checking if dialogue is in progress
```csharp
DialogueHandler handler = npc.GetComponent<DialogueHandler>();
if (handler.IsDialogueInProgress)
{
    // NPC is busy talking
}
```

### Creating a custom dialogue choice with validation
```csharp
var choice = new DialogueController.DialogueChoice
{
    ChoiceText = "Buy special item ($500)",
    Enabled = true,
    isValidCheck = (out string reason) =>
    {
        if (MoneyManager.Instance.cashBalance < 500)
        {
            reason = "Not enough cash";
            return false;
        }
        reason = "";
        return true;
    },
    onChoosen = new UnityEvent()
};
choice.onChoosen.AddListener(() => BuySpecialItem());
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.UI.ShowDialogue()` | Show dialogue (WIP) |
| `Api.UI.GetDialogueChoice()` | Get selected response (WIP) |

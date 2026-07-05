---
title: "Interaction System"
description: "Reference for the interaction system — InteractableObject, InteractionManager, prompts, and handlers"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Interaction System

## InteractableObject (`Il2CppScheduleOne.Interaction.InteractableObject`)
MonoBehaviour on any interactable world object.

### EInteractionType Enum
| Value | Description |
|-------|-------------|
| `Key_Press` | Press E/Interact key |
| `LeftMouse_Click` | Left-click |

### EInteractableState Enum
| Value | Description | Visual |
|-------|-------------|--------|
| `Default` | 0 | Normal interaction (white) |
| `Invalid` | 1 | Cannot interact (red) |
| `Disabled` | 2 | No prompt shown |
| `Label` | 3 | Label only (no interaction) |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `message` | `string` | Display text (e.g. `"[E] Open"`) |
| `interactionType` | `EInteractionType` | Input type |
| `interactionState` | `EInteractableState` | Current state |
| `MaxInteractionRange` | `float` | Max range (default: 5) |
| `RequiresUniqueClick` | `bool` | Prevent spam |
| `Priority` | `int` | Higher = prioritized |
| `displayLocationCollider` | `Collider` | Collider for label position |
| `displayLocationPoint` | `Transform` | Point for label position |
| `LimitInteractionAngle` | `bool` | Angle limitation |
| `AngleLimit` | `float` | Max angle (default: 90) |

### Events
| Event | Description |
|-------|-------------|
| `onHovered` | `UnityEvent` — Player looks at object |
| `onInteractStart` | `UnityEvent` — Interaction begins |
| `onInteractEnd` | `UnityEvent` — Interaction ends |

### Methods
| Method | Description |
|--------|-------------|
| `SetInteractionType(EInteractionType)` | Change input type |
| `SetInteractableState(EInteractableState)` | Change state (updates visual) |
| `SetMessage(string)` | Change display text |
| `Hovered()` | Virtual — called when player looks at |
| `StartInteract()` | Virtual — called on interaction |
| `EndInteract()` | Virtual — called when done |

---

## InteractionManager (`Il2CppScheduleOne.Interaction.InteractionManager`)
`Singleton<InteractionManager>`. Handles raycasting and dispatching interactions.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `RayRadius` | 0.075f | Thickness of interaction ray |
| `MaxInteractionRange` | 5f | Max interaction distance |
| `interactCooldown` | 0.1s | Cooldown between interactions |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `HoveredInteractableObject` | `InteractableObject` | Currently hovered (any state) |
| `HoveredValidInteractableObject` | `InteractableObject` | Currently hovered (valid) |
| `InteractedObject` | `InteractableObject` | Currently interacting with |
| `InteractKeyStr` | `string` | Display name for interact key |
| `CanDestroy` | `bool` (get/set) | Allow destroy mode |
| `Interaction_SearchMask` | `LayerMask` | Layers checked |
| `interactionSearchType` | `EInteractionSearchType` | Search mode |

### EInteractionSearchType Enum
Defines how interactions are searched (raycast, spherecast, etc).

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `InteractInput` | `InputActionReference` | The interact input |
| `messageColor_Default` | `Color` | Normal label color |
| `iconColor_Default` | `Color` | Default icon color |
| `messageColor_Invalid` | `Color` | Invalid label color |
| `icon_Key` | `Sprite` | Key press icon |
| `icon_LeftMouse` | `Sprite` | Left-click icon |
| `icon_Cross` | `Sprite` | Cross/blocked icon |

### Methods
| Method | Description |
|--------|-------------|
| `SetCanDestroy(bool)` | Toggle destroy mode |

---

## InteractableToggleable (`Il2CppScheduleOne.Interaction.InteractableToggleable`)
InteractableObject with toggle states (on/off).

### Methods
| Method | Description |
|--------|-------------|
| `Toggle()` | Switch state |
| `SetState(bool)` | Set specific state |

---

## NetworkedInteractableToggleable
Network-synced version of InteractableToggleable.

---

## IUsableInteractableObject
Interface for objects usable by NPCs.

---

## WorldSpaceLabel (`Il2CppScheduleOne.Interaction.WorldSpaceLabel`)
Floating world-space UI label.

---

## Usage Patterns

### Creating a custom interactable
```csharp
// Add component, configure in Awake/Start:
InteractableObject io = gameObject.AddComponent<InteractableObject>();
io.SetMessage("[E] Use my object");
io.SetInteractionType(InteractableObject.EInteractionType.Key_Press);
io.SetInteractableState(InteractableObject.EInteractableState.Default);
io.MaxInteractionRange = 3f;

// Hook events:
io.onHovered.AddListener(() => Debug.Log("Hovering!"));
io.onInteractStart.AddListener(() => Debug.Log("Interacting!"));
```

### Checking current hover
```csharp
InteractableObject hovered = Singleton<InteractionManager>.Instance.HoveredInteractableObject;
if (hovered != null && hovered == myObject)
{
    // Player is looking at my object
}
```

### Blocking interaction
```csharp
io.SetInteractableState(InteractableObject.EInteractableState.Disabled);
// or
io.SetInteractableState(InteractableObject.EInteractableState.Invalid);
```

### Custom interaction range
```csharp
// Override MaxInteractionRange per interactable
io.MaxInteractionRange = 10f; // Long-range interaction
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

Interaction-API in Entwicklung.

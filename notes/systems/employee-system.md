---
title: "Employee System"
description: "Reference for the employee system — Employee types, Botanist, Chemist, Cleaner, and assignment logic"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Employee System

## Employee (`Il2CppScheduleOne.Employees.Employee`)
Extends `NPC`. Base class for all hired employees.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `MAX_CONSECUTIVE_PATHING_FAILURES` | 5 | Max failed pathing before reporting |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `PaidForToday` | `bool` (SyncVar) | Whether daily wage was paid |
| `Type` | `EEmployeeType` (get) | Botanist, Handler, Chemist, Cleaner |
| `WorkSpeedController` | `FloatStack` | Work speed multiplier stack |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `SigningFee` | `float` | One-time hiring fee (default: 500) |
| `DailyWage` | `float` | Daily payment (default: 100) |
| `WaitOutside` | `IdleBehaviour` | Behaviour when waiting |
| `MoveItemBehaviour` | `MoveItemBehaviour` | Item transporting behaviour |
| `BedNotAssignedDialogue` | `DialogueContainer` | No bed dialogue |
| `NotPaidDialogue` | `DialogueContainer` | Unpaid dialogue |
| `WorkIssueDialogueTemplate` | `DialogueContainer` | Issue report dialogue |
| `FireDialogue` | `DialogueContainer` | Firing dialogue |
| `TransferDialogue` | `DialogueContainer` | Transfer dialogue |

### Nested Class: NoWorkReason
| Field | Type | Description |
|-------|------|-------------|
| `Reason` | `string` | Why employee can't work |
| `Fix` | `string` | How to resolve |
| `Priority` | `int` | Display priority |

### Methods
| Method | Description |
|--------|-------------|
| `GetWorkIssues()` | Returns `List<NoWorkReason>` |
| `AssignToProperty(Property)` | Assign employee to property |
| `GetAssignedProperty()` | Returns property reference |
| `SendHome()` | Send employee home |
| `Fire()` | Terminate employment |
| `Pay()` | Pay daily wage |

---

## EEmployeeType Enum
| Value | Index | Description |
|-------|-------|-------------|
| `Botanist` | 0 | Plant cultivation |
| `Handler` | 1 | Item handling/transport |
| `Chemist` | 2 | Chemical processing |
| `Cleaner` | 3 | Cleaning duties |

---

## Employee Subclasses

### Botanist (`Il2CppScheduleOne.Employees.Botanist`)
Handles planting, watering, harvesting.

### Chemist (`Il2CppScheduleOne.Employees.Chemist`)
Operates chemistry stations, mixing stations, lab ovens.

### Cleaner (`Il2CppScheduleOne.Employees.Cleaner`)
Cleans trash, debris, and maintains property.

### Packager (`Il2CppScheduleOne.Employees.Packager`)
Operates packaging stations, brick presses.

---

## EmployeeHome (`Il2CppScheduleOne.Employees.EmployeeHome`)
MonoBehaviour on employee bed/home object.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `AssignedEmployee` | `Employee` | Employee sleeping here |

---

## EmployeeManager (`Il2CppScheduleOne.Employees.EmployeeManager`)
Singleton managing all hired employees.

### Methods
| Method | Description |
|--------|-------------|
| `GetAllEmployees()` | Returns all hired employees |
| `GetEmployeesAtProperty(Property)` | Employees assigned to a property |

---

## NPCResponses_Employee (`Il2CppScheduleOne.Employees.NPCResponses_Employee`)
Dialogue responses specific to employee NPCs.

---

## Configuration System (Management folder)

Employee configurations are controlled via `IConfigurable`:

| Config Type | Purpose |
|-------------|---------|
| `BotanistConfiguration` | Plant assignments, harvest targets |
| `ChemistConfiguration` | Recipe assignments, station routing |
| `CleanerConfiguration` | Clean area preferences |
| `PackagerConfiguration` | Packaging type, output routing |
| `LabOvenConfiguration` | Meth cooking setup |
| `CauldronConfiguration` | Mixing/cooking setup |
| `BrickPressConfiguration` | Brick pressing setup |
| `MixingStationConfiguration` | Mixing recipe setup |

### Transit Route System
| Class | Description |
|-------|-------------|
| `TransitRoute` | Base route between configurable inputs/outputs |
| `AdvancedTransitRoute` | Full source → destination route |
| `IConfigurable` | Interface for configurable objects |
| `ConfigField` | Serialized configuration field |

---

## Work Speed System
```csharp
// Speed modifiers stack multiplicatively
employee.WorkSpeedController.AddControl("mod_id", priority: 5, speed: 1.5f);
// Remove when done
employee.WorkSpeedController.RemoveControl("mod_id");
```

---

## Usage Patterns

### Getting all employee issues
```csharp
Employee employee = GetComponent<Employee>();
foreach (var issue in employee.GetWorkIssues())
{
    MelonLogger.Msg($"Issue: {issue.Reason} - Fix: {issue.Fix}");
}
```

### Checking if employee is paid
```csharp
if (!employee.PaidForToday)
{
    // Employee won't work until paid
}
```

### Paying employee
```csharp
if (InstanceFinder.IsServer)
{
    // Employee payment handled by game systems
    // Trigger via Property → Business → daily wage
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Employees.Hire()` | Hire an employee |
| `Api.Employees.Fire()` | Fire an employee |
| `Api.Employees.GetEmployeeCount()` | Get employee count |
| `Api.Employees.GetEmployeeType()` | Get employee type |

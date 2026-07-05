---
title: "S1Toolkit.Modules (class-based layer)"
description: "Create custom NPCs, quests, phone apps, and saveable data by inheriting base classes"
status: "stable"
version: "1.0.0"
applies_to: "Schedule I v0.4.3+ (Il2Cpp, MelonLoader)"
---

# S1Toolkit.Modules — Class-Based Modding Layer

While `S1Toolkit.Api` lets you *read and change existing game state* with static calls (`Api.Money.Add(1000)`), **S1Toolkit.Modules** lets you *add new content* by inheriting base classes: custom quests, phone apps, NPCs, messages, and data that persists into the save file.

> **⚠️ Advanced layer** — targets human developers and strong LLMs. Small models should use the static `S1.*` / `Api.*` API instead; see [`Tools/LLM-CONTEXT.md`](../Tools/LLM-CONTEXT.md).

> **Attribution** — adapted from [S1API](https://github.com/ifBars/S1API) (MIT) by KaBooMa & ifBars, reduced to a single build target (Il2Cpp + MelonLoader, `net6.0`). See `mod-source/S1Toolkit-modules/LICENSE-S1API.txt`.

## Requirements

- **.NET 8+ SDK** to build (the source uses C# 12; the output still targets `net6.0`)
- Reference `S1Toolkit.Modules.dll` in your mod project (ships in the release zip)
- `S1Toolkit.Modules.dll` must be in the game's `Mods/` folder at runtime

## Quickstart

A complete compiling example lives in [`mod-source/ModulesExample/`](https://github.com/domi7602/S1Toolkit/tree/main/mod-source/ModulesExample).

### 1. Lifecycle events

```csharp
using MelonLoader;
using S1Toolkit.Modules.Lifecycle;

public class Main : MelonMod
{
    public override void OnInitializeMelon()
    {
        GameLifecycle.OnPreLoad += () =>
        {
            // Before save data loads — register custom items here.
        };
        GameLifecycle.OnLoadComplete += () =>
        {
            MelonLogger.Msg("Save loaded — world is ready.");
        };
    }
}
```

### 2. Custom quest

```csharp
using S1Toolkit.Modules.Quests;

public class ExampleQuest : Quest
{
    protected override string Title => "Example Quest";
    protected override string Description => "Created by S1Toolkit.Modules.";

    protected override void OnCreated()
    {
        base.OnCreated();
        if (QuestEntries.Count == 0)
            AddEntry("Do the example thing.");
    }
}

// Start it once the save is loaded:
GameLifecycle.OnLoadComplete += () => QuestManager.CreateQuest<ExampleQuest>();
```

### 3. Custom phone app

```csharp
using S1Toolkit.Modules.PhoneApp;
using UnityEngine;

public class ExampleApp : PhoneApp
{
    protected override string AppName => "example_app";
    protected override string AppTitle => "Example";
    protected override string IconLabel => "EX";
    protected override string IconFileName => "example_icon.png";

    protected override void OnCreatedUI(GameObject container)
    {
        // Build your app UI here (container is the app's root panel).
    }
}
```

### 4. Saveable data

Fields marked `[SaveableField]` on a `Saveable` subclass (which `Quest` and `NPC` already are) persist into the save file automatically:

```csharp
using S1Toolkit.Modules.Saveables;

public class MyQuest : Quest
{
    [SaveableField("times_completed")]
    private int _timesCompleted;
    // ...
}
```

## Module overview

| Namespace (`S1Toolkit.Modules.*`) | What you get |
|---|---|
| `Lifecycle` | `GameLifecycle.OnPreLoad` / `OnLoadComplete` |
| `Quests` | `Quest` base class, `QuestManager.CreateQuest<T>()`, entries with map POIs |
| `PhoneApp` | `PhoneApp` base class — own icon + UI on the in-game phone |
| `Entities` | `NPC` base class — fully custom NPCs (appearance, schedule, dialogue) |
| `Saveables` | `[SaveableField]` — automatic save-file persistence |
| `Messaging`, `PhoneCalls` | Custom text conversations and calls |
| `Products`, `Items`, `Shops` | Custom products, items, shop listings |
| `Economy`, `Money`, `GameTime`, `Map`, `Vehicles`, `Storages`, `Law`, … | Wrappers over the corresponding game systems |

## Building

```powershell
cd mod-source/S1Toolkit-modules
dotnet build                                     # game at default Steam path
dotnet build -p:GameDir="D:\Games\Schedule I"    # custom install path
```

The upstream Mono/BepInEx `#if` branches remain in the source but are not compiled (`DefineConstants` is fixed to `IL2CPPMELON`) — do not remove them, they keep future upstream merges cheap.

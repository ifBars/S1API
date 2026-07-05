---
title: "Mod Patterns & Techniques"
description: "Collection of reusable modding patterns from the community — modular frameworks, phone integration, survival systems, and compatibility tools"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
---

# Mod Patterns & Techniques

This document catalogs reusable design patterns extracted from real Schedule I mods.
Each pattern includes the architectural approach, key code examples, and the mod that pioneered it.

## Quick-Start: Copy-Paste Patterns

Ready-made code snippets for common mod patterns live in `Tools/patterns/`:

| Pattern | File | Description |
|---|---|---|
| Infinite Money | `infinite-money.cs` | Add money every second |
| Full Health/Energy | `full-health-energy.cs` | Keep HP + energy at 100 |
| Give Item | `give-item.cs` | Give any item by ID |
| Spawn Vehicle | `spawn-vehicle.cs` | Spawn a vehicle (F5) |
| Teleport | `teleport-to-marker.cs` | Jump to coordinates (F6) |
| Time Control | `time-control.cs` | Set the time of day (F7/F8) |
| Harvest Ready | `instant-grow.cs` | Harvest all ready plants (F9) |
| No Police | `no-police.cs` | Permanently clear wanted status |
| NPC Friend | `npc-friend.cs` | Max relationship with all NPCs |
| Unlock Recipes | `unlock-recipes.cs` | Discover all products |
| Freeze Time | `freeze-time.cs` | Freeze time (F11) |
| Kill NPCs | `kill-all-npcs.cs` | Kill all NPCs (F12) |
| Complete Quests | `complete-quests.cs` | Complete active quests (F4) |
| Notify Events | `notify-on-event.cs` | Notifications on events |

Each snippet is 10-20 lines and uses only `Api.*` calls — no Il2Cpp knowledge required.

---

## 1. Modular JSON-Config Framework (Lithium_fork)

!!! tip "Source"
    **Lithium_fork** by EVB (fork of Lithium by DerTomDerTwitch).
    A modular balancing framework where every feature is an independent, toggleable module.

### Architecture

```
Lithium_fork.dll
├── Core.cs                    — Main MelonMod, loads all modules
├── Modules/
│   ├── ModuleBase.cs          — Abstract base for all modules
│   ├── ModuleConfiguration.cs — JSON config base class
│   ├── PlantGrowth/           — One folder per module
│   │   ├── ModPlants.cs       — Module implementation
│   │   ├── ModPlantsConfiguration.cs  — JSON config class
│   │   └── Patches/           — Harmony patches for this module
│   ├── MixingStations/
│   ├── Customers/
│   ├── Employees/
│   ├── Shops/
│   └── ... (18 modules total)
└── UserData/Lithium/          — Auto-generated JSON configs (at runtime)
    ├── Plants.json
    ├── MixingStation.json
    └── ...
```

### ModuleBase

Every module inherits from `ModuleBase` which provides:

```csharp
public abstract class ModuleBase
{
    public abstract string ConfigFileName { get; }    // e.g., "Plants.json"
    public abstract bool Enabled { get; set; }         // Toggle via JSON
    public abstract void ApplyPatches(Harmony harmony); // Register Harmony patches
    public virtual void LoadConfiguration() { }         // JSON deserialization
    public virtual void OnSceneLoad() { }               // Scene reload handler
}
```

### Module Configuration

Each module has a dedicated JSON config class:

```csharp
public class ModPlantsConfiguration : ModuleConfiguration
{
    public bool Enabled { get; set; } = false;

    public PlantsPotsSection Plants { get; set; } = new();
    public PlantsShroomsSection Shrooms { get; set; } = new();

    // Legacy fallbacks
    public float GrowthModifier { get; set; } = 1.0f;
    public float WaterDrainModifier { get; set; } = 1.0f;
}

public class PlantsPotsSection
{
    public string _Note { get; set; } = "POT-BASED PLANTS (Weed/Coca/Meth)...";
    public float GrowthSpeed { get; set; } = 1.0f;
    public float WaterDrainModifier { get; set; } = 1.0f;
}
```

Resulting JSON:

```json
{
  "Enabled": true,
  "Plants": {
    "_Note": "POT-BASED PLANTS: GrowthSpeed 1.0 = normal",
    "GrowthSpeed": 1.0,
    "WaterDrainModifier": 1.0
  },
  "Shrooms": {
    "_Note": "MUSHROOMS: separate config",
    "GrowthSpeed": 1.0,
    "WaterDrainModifier": 1.0
  }
}
```

### Module Registration in Core

```csharp
public sealed class Core : MelonMod
{
    private List<ModuleBase> _modules = new()
    {
        new ModPlants(),
        new ModMixingStation(),
        new ModCustomers(),
        new ModEmployees(),
        // ... 18 modules
    };

    public override void OnInitializeMelon()
    {
        foreach (var module in _modules)
        {
            module.LoadConfiguration();
            if (module.Enabled)
                module.ApplyPatches(HarmonyInstance);
        }
    }
}
```

### Key Benefits

- **Toggleable**: Users enable only what they need via JSON
- **Isolated failures**: One broken module doesn't affect others
- **JSON-configurable**: No recompilation needed for tuning
- **Self-documenting**: `_Note` fields in JSON explain each setting
- **Auto-generated**: Default configs created on first run via `MelonEnvironment.UserDataDirectory`

---

## 2. Native Phone App Integration (ExtendedEmployees)

!!! tip "Source"
    **ExtendedEmployees** by Leigue.
    Adds employee management directly into the in-game phone with vanilla-style UI.

### Architecture

Instead of creating custom GUI windows (IMGUI/UIToolkit), this pattern hooks into the
game's existing phone messaging system to create a native-feeling interface:

```
Player opens Phone → Employee Contact → MSGConversation with custom choices
   Each choice triggers: hire/fire/upgrade/pay employees via Harmony patches
   UI is entirely vanilla dialogue boxes — no custom windows
```

### Phone Contact Creation

```csharp
private IEnumerator CreatePhoneContact()
{
    // Wait for game initialization
    while (PhoneManager.Instance == null)
        yield return null;

    // Create or find the employee management contact
    var contact = new MSGConversation(
        "EmployeeManagement",
        "My Employees",
        icon: Resources.Load<Sprite>("Icons/employees")
    );

    // Add dialogue choices that trigger mod actions
    contact.AddChoice("View Employees", () => ShowEmployeeList());
    contact.AddChoice("Hire New", () => ShowHiringCandidates());
    contact.AddChoice("Payroll", () => ShowPayroll());
}
```

### Trait System

Employees get randomized traits that affect gameplay:

```csharp
public enum TraitType
{
    HardWorker,        // Max work speed 7x (instead of 5x)
    EfficientWalker,   // Faster movement speed
    Loyal,             // -10% wage
    Motivated,         // 1.5x work speed
    Veteran,           // 2.0x work + walk speed
    Perfectionist,     // Quality bonus
    Lazy,              // Max work speed capped at 2x
    SlowWalker,        // Reduced movement
    Demanding,         // Higher wage expectations
    Greedy,            // +35% wage
    Clumsy             // Chance to fail actions
}

public class EmployeeTrait
{
    public string Id;
    public string DisplayName;
    public float WorkSpeedModifier;
    public float WalkSpeedModifier;
    public float WageModifier;
    public float MaxWorkSpeedCap; // 0 = no cap
}
```

### Trait Generation with Conflict Prevention

```csharp
private static void GenerateTraits(EmployeeUpgradeData data)
{
    // Conflicting trait pairs that cannot appear together
    var conflicts = new (TraitType, TraitType)[]
    {
        (TraitType.Loyal, TraitType.Greedy),
        (TraitType.HardWorker, TraitType.Lazy),
        (TraitType.Veteran, TraitType.Motivated),
        (TraitType.EfficientWalker, TraitType.SlowWalker),
    };

    // Generate 1-3 random traits
    int traitCount = UnityEngine.Random.Range(1, 4);
    for (int i = 0; i < traitCount; i++)
    {
        var trait = GetRandomTrait();
        // Check conflicts with already assigned traits
        if (!HasConflict(trait, data.Traits, conflicts))
            data.Traits.Add(trait.Id);
    }
}
```

### Employee Data Persistence

```csharp
public class SaveContainer
{
    public Dictionary<string, EmployeeUpgradeData> EmployeeData { get; set; }

    public void Save()
    {
        string path = Path.Combine(MelonEnvironment.UserDataDirectory,
            "ExtendedEmployees", "EmployeeData.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
    }

    public static SaveContainer Load()
    {
        string path = Path.Combine(MelonEnvironment.UserDataDirectory,
            "ExtendedEmployees", "EmployeeData.json");
        if (File.Exists(path))
            return JsonConvert.DeserializeObject<SaveContainer>(File.ReadAllText(path));
        return new SaveContainer { EmployeeData = new Dictionary<string, EmployeeUpgradeData>() };
    }
}
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Phone contact instead of custom UI | No external windows, no input system conflicts, vanilla feel |
| Per-employee JSON save | Survives scene reloads, survives game updates |
| Trait conflicts at generation | Prevents impossible/imbalanced combinations |
| `FindObjectsOfType<Employee>()` every 2s | Catches newly hired employees without patching hire events |

---

## 3. MelonPlugin — Branch Detection & Mod Compatibility (OTCLoader)

!!! tip "Source"
    **OTC Loader** by hdlmrell.
    A MelonLoader plugin that auto-detects the game branch and disables incompatible DLLs.

### Plugin vs Mod

OTCLoader is a **MelonPlugin** (not `MelonMod`), which means:
- It runs **before** any mods are loaded
- It can inspect and modify the `Mods/` folder before mods initialize
- It goes in the `Plugins/` folder, not `Mods/`

```csharp
public class LoaderPlugin : MelonPlugin
{
    public override void OnPreInitialization()
    {
        // Runs before any MelonMod.OnInitializeMelon()
        DetectBranch();
        ScanAndDisableIncompatibleMods();
    }
}
```

### Branch Detection — Two-Stage Strategy

```csharp
private static Branch DetectBranch()
{
    // Stage 1: Check MelonLoader's own branch indicator
    if (MelonUtils.IsIl2Cpp)
        return Branch.IL2CPP;

    // Stage 2: Mono.Cecil inspection of Assembly-CSharp
    string assemblyPath = Path.Combine(
        MelonEnvironment.GameDirectory,
        "Schedule I_Data", "Managed", "Assembly-CSharp.dll");

    using (var module = Mono.Cecil.ModuleDefinition.ReadModule(assemblyPath))
    {
        foreach (var type in module.Types)
        {
            if (type.Namespace.StartsWith("Il2Cpp")) return Branch.IL2CPP;
            if (type.Namespace.StartsWith("ScheduleOne")) return Branch.Mono;
        }
    }
    return Branch.Unknown;
}
```

### Mod DLL Inspection

```csharp
private static ModBranch GetDllBranch(string dllPath)
{
    string fileName = Path.GetFileNameWithoutExtension(dllPath).ToLower();

    // Stage 1: Filename keywords (fast)
    if (fileName.Contains("il2cpp")) return ModBranch.IL2CPP;
    if (fileName.Contains("mono")) return ModBranch.Mono;

    // Stage 2: Mono.Cecil assembly inspection (accurate)
    try
    {
        using (var module = Mono.Cecil.ModuleDefinition.ReadModule(dllPath))
        {
            foreach (var type in module.Types)
            {
                if (type.Namespace.StartsWith("Il2CppScheduleOne")) return ModBranch.IL2CPP;
                if (type.Namespace.StartsWith("ScheduleOne")) return ModBranch.Mono;
            }
        }
    }
    catch { /* native DLL, locked file, or branch-agnostic */ }

    return ModBranch.Unknown; // Leave untouched
}
```

### Disabling Incompatible Mods

```csharp
private static void DisableDll(string dllPath)
{
    string disabledPath = dllPath + ".off";
    if (!File.Exists(disabledPath))
        File.Move(dllPath, disabledPath);
}

private static void RestorePreviouslyDisabled()
{
    foreach (string offFile in Directory.GetFiles(modsFolder, "*.dll.off"))
    {
        string original = offFile.Substring(0, offFile.Length - 4);
        File.Move(offFile, original);
    }
}
```

### Configurable Whitelist

```csharp
public class LoaderConfig
{
    public List<string> Whitelist { get; set; } = new();
    public bool FirstTimeSetupComplete { get; set; } = false;

    public bool IsWhitelisted(string dllName)
    {
        return Whitelist.Contains(dllName, StringComparer.OrdinalIgnoreCase);
    }
}
```

### Key Benefits

- **Prevents crashes** before they happen (disables wrong-branch DLLs)
- **Auto-restore** on branch switch (`.dll.off` renamed back)
- **Dual-branch support** for mod authors without writing detection logic
- **Whitelist** for advanced users who know what they're doing

---

## 4. Player State & Survival System (Flatline)

!!! tip "Source"
    **Flatline** by XO_WithSauce.
    A hardcore survival mod adding hunger, thirst, temperature, diseases, and injuries.

### Architecture

The mod extends the player with additional state tracked per session and saved
via a custom JSON file:

```
FlatlinePlayer
├── HealthData (HP, hunger, thirst, stamina)
├── Disease[] (active diseases with progression)
├── Ingestible[] (food/drink effects)
└── PropertyTemperatureController (per-property temperature)
```

### Player State Extension

```csharp
public class FlatlinePlayerData
{
    public float Health = 100f;
    public float Hunger = 100f;
    public float Thirst = 100f;
    public float Stamina = 100f;
    public float BodyTemperature = 37f;
    public List<DiseaseData> ActiveDiseases = new();
}

public static class FlatlinePlayer
{
    private static FlatlinePlayerData _data = new();

    public static void Tick()
    {
        // Deplete hunger/thirst over time
        _data.Hunger -= Time.deltaTime * 0.1f;
        _data.Thirst -= Time.deltaTime * 0.15f;

        // Apply temperature effects
        if (_data.BodyTemperature < 35f || _data.BodyTemperature > 39f)
            _data.Health -= Time.deltaTime * 0.5f;

        // Tick all active diseases
        foreach (var disease in _data.ActiveDiseases)
            disease.Tick();
    }
}
```

### Disease System with Inheritance

```csharp
public abstract class Disease
{
    public string Name;
    public float Severity;       // 0.0 to 1.0
    public float ProgressionRate;
    public abstract void ApplyEffects(FlatlinePlayerData player);
    public abstract bool CanCure { get; }
}

public class Fever : Disease
{
    public override void ApplyEffects(FlatlinePlayerData player)
    {
        player.BodyTemperature += 0.1f * Severity * Time.deltaTime;
        if (Severity > 0.5f)
            player.Stamina -= Time.deltaTime * 2f;
    }
    public override bool CanCure => true;
}

public class Cancer : Disease
{
    public override void ApplyEffects(FlatlinePlayerData player)
    {
        player.Health -= 0.3f * Severity * Time.deltaTime;
    }
    public override bool CanCure => false; // Requires hospital
}
```

### Ingestible System (Food/Drink)

```csharp
public abstract class Ingestible
{
    public abstract void OnConsume(FlatlinePlayerData player);
}

public class IngestibleBanana : Ingestible
{
    public override void OnConsume(FlatlinePlayerData player)
    {
        player.Hunger = Mathf.Min(100f, player.Hunger + 25f);
        player.Thirst = Mathf.Min(100f, player.Thirst + 10f);
        player.Stamina = Mathf.Min(100f, player.Stamina + 15f);
    }
}

public class IngestibleEnergyDrink : Ingestible
{
    public override void OnConsume(FlatlinePlayerData player)
    {
        player.Thirst = Mathf.Min(100f, player.Thirst + 40f);
        player.Stamina = Mathf.Min(100f, player.Stamina + 50f);
        // Side effect: slight fever
        player.BodyTemperature += 0.3f;
    }
}
```

### Harmony Patches

```csharp
// Patch player product consumption
[HarmonyPatch(typeof(Player), nameof(Player.ConsumeProduct))]
[HarmonyPrefix]
static void OnConsumeProduct(Player __instance, ProductInstance product)
{
    var ingestible = Ingestible.FromProduct(product);
    ingestible?.OnConsume(FlatlinePlayer.Data);
}

// Patch fall damage
[HarmonyPatch(typeof(Player), nameof(Player.ReceiveImpact))]
[HarmonyPrefix]
static void OnFallDamage(Player __instance, Impact impact)
{
    if (impact.force > 500f)
        FlatlinePlayer.Data.Health -= impact.force * 0.01f;
}

// Patch daily health pass
[HarmonyPatch(typeof(PlayerHealth), "MinPass")]
[HarmonyPostfix]
static void OnMinPass()
{
    FlatlinePlayer.Tick();
    FlatlinePlayer.CheckDeath();
}
```

### Temperature System per Property

```csharp
public class PropertyTemperatureController
{
    public void UpdatePropertyTemperatures()
    {
        foreach (var property in Property.Properties)
        {
            float baseTemp = property.AmbientTemperature;
            float outdoorTemp = GetOutdoorTemperature();

            if (property.IsInterior)
            {
                // Interior holds temperature better
                property.AmbientTemperature = Mathf.Lerp(
                    baseTemp, outdoorTemp, 0.1f * Time.deltaTime);
            }
            else
            {
                property.AmbientTemperature = outdoorTemp;
            }
        }
    }

    private float GetOutdoorTemperature()
    {
        // Based on time of day, weather, season
        float hour = TimeManager.CurrentTime / 100f;
        return 25f - Mathf.Abs(hour - 14f) * 2f; // Peak at 14:00
    }
}
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Static player data + JSON save | Simple, no Il2Cpp type registration needed |
| Abstract Disease base class | Easy to add new diseases without modifying core loop |
| Ingestible registry by product ID | Works with any mod that adds new products |
| Harmony Postfix on MinPass | Runs once per in-game minute — perfect for survival ticks |
| Per-property temperature | Integrates with existing Property system |

---

## 5. S1Toolkit API

All patterns use the [S1Toolkit API](../api/api-reference.md). For more complex needs see the [full API reference](../api/api-reference.md) (29 subsystems, ~190 methods).

---

## 6. Cross-References

| Pattern | Used By | Documented In |
|---------|---------|---------------|
| Synthetic Grids | SidewalkEconomy | [Building System](../systems/building-system.md#advanced-synthetic-grids-placing-outside-owned-properties) |
| Custom Interiors | Ndrangheta | [Building System](../systems/building-system.md#advanced-custom-interiors-remodeling-existing-buildings) |
| GLTF Import | BigPimpin, S1MAPI | [Building System](../systems/building-system.md#advanced-gltfglb-model-import-custom-3d-buildings-without-asset-bundles), [Building API](../guide/building-api.md) |
| Tile Injection | PropertyRenovations | [Building System](../systems/building-system.md#advanced-tile-injection-via-harmony-expanding-existing-property-grids) |
| World Editing | OverpassMod | [Building System](../systems/building-system.md#advanced-world-editing-finding-and-modifying-scene-objects) |
| Modular JSON Config | Lithium_fork | This document |
| Phone App Integration | ExtendedEmployees | This document |
| MelonPlugin/Branch Detection | OTCLoader | This document |
| Player State/Survival | Flatline | This document |
| S1MAPI Building API | S1MAPI | [Building API](../guide/building-api.md) |
| Tools/patterns/ | 15 vorgefertigte Code-Schnipsel | scaffold.ps1, new-mod.ps1 |

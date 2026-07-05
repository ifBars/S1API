---
title: "Complete Modding Reference"
description: "Single-file summary of all documentation — project structure, core systems, class references, and indexes"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.5+"
---

# Complete Modding Reference

> Single-file summary of all docs. Game source: Il2Cpp, Unity 2022+ URP, FishNet + FishySteamworks.

---

## 1. PROJECT STRUCTURE

```
Schedule 1 source/scripts/Assembly-CSharp/ScheduleOne/
├── Economy/        — Customer, Dealer, Supplier, DeadDrop, ContractReceipt
├── ItemFramework/  — ItemDefinition, ItemInstance, ItemSlot, ItemFilter
├── PlayerScripts/  — Player, PlayerMovement, PlayerInventory, PlayerCrimeData
├── UI/             — HUD, PauseMenu, Phone apps, DialogueCanvas, StorageMenu
├── NPCs/           — NPC, NPCManager, NPCMovement, NPCScheduleManager
├── Product/        — ProductDefinition, ProductManager, MixRecipeData
├── Persistence/    — SaveManager, LoadManager, ItemDeserializer, ISaveable
├── Management/     — EntityConfiguration, ConfigField, TransitRoute
├── Property/       — Property, PropertyManager, Business
├── ObjectScripts/  — ChemistryStation, Pot, GrowLight, PackagingStation
├── Quests/         — Quest, QuestManager, Contract, StateMachine
├── Combat/         — CombatManager, RangedWeapon, ReticleController
├── Doors/          — PivotDoor, SlidingDoor, StaticDoor
├── Map/            — Map, POI, NPCPoI, MapPositionUtility
├── Equipping/      — Equippable, Equippable_RangedWeapon, ViewmodelAvatar
├── AvatarFramework/ — Avatar, AvatarSettings, MugshotGenerator
├── Growing/        — Plant, GrowContainer, SeedDefinition
├── Building/       — BuildManager, BuildStart/Update/Stop
├── Clothing/       — ClothingDefinition, ClothingInstance
├── Employees/      — Employee, Botanist, Chemist, Cleaner
├── Interaction/    — InteractableObject, InteractionManager
├── Vehicles/       — LandVehicle, VehicleManager, VehicleSeat
├── Networking/     — Lobby, AutoNetworkStart, TransportInitializer
├── Messaging/      — MessagingManager, MSGConversation, Message
├── Levelling/      — LevelManager, ERank (11 ranks)
├── Law/            — LawManager, Crime (17 types), CurfewManager
├── Police/         — PoliceOfficer, Investigation, Offense
├── GameTime/       — TimeManager, GameDateTime, EDay
├── Money/          — MoneyManager, Transaction
├── DevUtilities/   — Console, Singleton, Settings, SceneUtility
└── Dialogue/       — DialogueController, DialogueHandler, DialogueContainer
```

---

## 2. CORE SYSTEMS

### 2.1 Registry — Central Item Lookup
```csharp
Registry.GetItem(string id)          // → ItemDefinition or null
Registry.ItemExists(string id)        // → bool
Registry.Instance.GetAllItems()       // → Il2CppList<ItemDefinition>
```
Items stored by string ID (e.g., `"ogkush"`, `"baggies"`, `"weed_seed"`).

### 2.2 TimeManager (`Il2CppScheduleOne.GameTime.TimeManager`)
```csharp
NetworkSingleton<TimeManager>.Instance
  .CurrentTime      // int, HHMM format (0=midnight, 1200=noon, 2359=11:59PM)
  .CurrentDay       // EDay enum (Monday-Sunday)
  .ElapsedDays      // int, total days elapsed
  .IsNight          // bool, 1800-0600
  .IsEndOfDay       // bool, true at 400
  .TimeSpeedMultiplier  // float, default 1.0
  .onMinutePass     // Action — each game minute
  .onHourPass       // Action — each hour
  .onDayPass        // Action — new day
  .onSleepStart     // Action — sleep transition begins
  .onSleepEnd       // Action — sleep transition ends
```
Static utils: `IsGivenTimeWithinRange(time, min, max)`, `Get12HourTime()`, `GetMinSumFrom24HourTime()`.

### 2.3 GameInput (`Il2CppScheduleOne`)
```csharp
GameInput.GetButton(ButtonCode code)     // poll button state
GameInput.GetButtonDown(ButtonCode code)
GameInput.RegisterExitListener(ExitDelegate, int priority)
GameInput.DeregisterExitListener(ExitDelegate)
```
Key enums: `ButtonCode.PrimaryClick`, `SecondaryClick`, `Back`, `Escape`, `Interact`, `Reload`.
Exit flow: `OnBack()` → `Exit(ExitType.RightClick)` → iterates `exitListeners` by priority.

### 2.4 MoneyManager (`Il2CppScheduleOne.Money`)
```csharp
MoneyManager.Instance
  .cashBalance      // float
  .onlineBalance    // float (SyncVar)
  .ChangeCashBalance(float change, bool visualize, bool playCashSound)
  .GetNetWorth()    // → float
```

### 2.5 PlayerInventory (`Il2CppScheduleOne.PlayerScripts`)
```csharp
PlayerInventory.Instance.GetAllInventorySlots()  // → Il2CppList<ItemSlot> (hotbar + backpack)
Player.Inventory                                  // → subset (hotbar only — use GetAllInventorySlots for full)
```

### 2.6 ItemInstance Hierarchy (0.44+)
```
ItemInstance (abstract) — ID, Name, Quantity, Definition, GetCopy(), GetItemData()
  └─ QualityItemInstance — has quality field
       └─ ProductItemInstance — has quality + packaging (PackagingID, AppliedPackaging, Amount)
```
Always use `TryCast<ProductItemInstance>()` — a `QualityItemInstance` is NOT always a `ProductItemInstance`.

### 2.7 ItemSlot (`Il2CppScheduleOne.ItemFramework`)
```csharp
ItemSlot
  .ItemInstance     // stored item
  .Quantity         // stack quantity
  .IsLocked / IsAddLocked / IsRemovalLocked
  .HardFilters      // List<ItemFilter>
  .SetStoredItem(ItemInstance, bool replicate)  // direct set, bypasses filters
  .InsertItem() / AddItem()                     // may check filters
  .GetCapacityForItem()                         // checks filters + capacity
  .DoesItemMatchHardFilters()                   // checks all hard filters
```

### 2.8 ProductManager (`Il2CppScheduleOne.Product`)
```csharp
ProductManager.Instance
  .ProductPrices       // Dictionary<ProductDefinition, float> — live prices
  .DiscoveredProducts  // discovered recipes
  .ListedProducts      // active product listings
```

### 2.9 NPC (`Il2CppScheduleOne.NPCs.NPC`)
```csharp
NPC
  .ID, .FirstName, .LastName, .fullName
  .MugshotSprite, .RelationData
  .CurrentBuilding, .LastEnteredDoor
  .Movement (NPCMovement) — SetDestination(), Warp()
  .Awareness — VisionCone, noticePlayerCrimes
```
Lookup: `NPCManager.GetNPC(string id)`.

### 2.10 Quest System
```csharp
QuestManager.CreateQuest(prefab).Begin(true)
QuestManager.GetQuest(string guid)  // → Quest
EQuestState: Inactive, Active, Completed, Failed, Expired, Cancelled
Quest.State, .Entries, .Title, .CompletionXP
```
Known quest GUIDs: `getting_started`, `we_need_to_cook`, `warehouse`, `defeat_cartel` (25 total).

### 2.11 LevelManager (`Il2CppScheduleOne.Levelling`)
```csharp
Singleton<LevelManager>.Instance
  .Rank      // ERank (Street_Rat=0 → Kingpin=10)
  .Tier      // 1-5 per rank
  .XP        // current XP
  .TotalXP   // lifetime XP
  .XPToNextTier  // float
  .AddXP(int)    // ServerRpc
```
Unlockables: `Unlockables` dictionary per `FullRank`.

### 2.12 BuildManager (`Il2CppScheduleOne.Building`)
```csharp
BuildManager.Instance
  .isBuilding           // bool
  .currentBuildHandler  // GameObject
  .StartBuilding(ItemInstance)
  .StopBuilding()
```
3-phase state machine: `BuildStart_Base` → `BuildUpdate_Base` → `BuildStop_Base`.
Subclasses: `BuildStart_Grid`, `BuildUpdate_Surface`, etc.

### 2.13 GridItem / BuildableItem
```csharp
GridItem : NetworkBehaviour
  .GUID, .OwnerGrid, .CoordinatePairs, .NetworkObject
  .SetGUID(string)                   // sets field only
  .InitializeGridItem(instance, grid, coord, rotation, GUID)  // full init + GUID reg
BuildableItem : GridItem
  .OutlineEffect (Outlinable), .BuildPoint
```

### 2.14 CRITICAL: Never AddComponent for Game Types
`BuildableItem`, `GridItem`, `PlaceableStorageEntity`, `FootprintTile` etc. — their `Awake()` methods NRE on minimal objects.
**Always `Instantiate(prefab)` from existing scene objects**, never `AddComponent<T>()`.

---

## 3. MODDING SETUP

### Project Requirements
| Backend | Framework | References |
|---------|-----------|------------|
| Mono | .NETStandard 2.1 | MelonLoader/net35, Schedule I_Data/Managed |
| Il2Cpp | .NET 6.0 | MelonLoader/net6, MelonLoader/Il2CppAssemblies |

### NuGet
`dotnet add package S1API.Forked` — cross-backend API wrappers.

### MelonMod Lifecycle
```csharp
[assembly: MelonInfo(typeof(Mod), "Name", "1.0", "Author")]
[assembly: MelonGame("TVGS", "Schedule I")]
public class Mod : MelonMod {
    override void OnInitializeMelon()     // settings, prefs
    override void OnLateInitializeMelon() // Il2Cpp type registration
    override void OnSceneWasLoaded(int, string)  // init on "Main"
    override void OnUpdate()              // per frame
    override void OnLateUpdate()           // post-frame
}
```

### Harmony Patching
```csharp
[HarmonyPatch(typeof(Target), "Method")]
class Patch {
    static void Prefix(Target __instance, ref int __result) { }  // before
    static void Postfix(Target __instance, int __result) { }     // after
    static Exception Finalizer(Exception __exception) { }        // on error
}
```
Special params: `__instance` (instance), `__result` `ref` (return), `__state` (cross-patch).
Prefix `return false` skips original (Postfixes still run in Harmony 2.x!).

---

## 4. Il2Cpp SPECIFICS

### Namespace Prefix
```csharp
// Mono:  using ScheduleOne.PlayerScripts;
// Il2Cpp: using Il2CppScheduleOne.PlayerScripts;
```

### Collections
```csharp
// Mono:  System.Collections.Generic.List<T>
// Il2Cpp: Il2CppSystem.Collections.Generic.List<T>
// LINQ workaround: list._items.FirstOrDefault()
```

### Type Registration
```csharp
[RegisterTypeInIl2Cpp]
public class MyType {
    public MyType(IntPtr ptr) : base(ptr) { }  // REQUIRED
    public MyType() : base(ClassInjector.DerivedConstructorPointer<MyType>()) {
        ClassInjector.DerivedConstructorBody(this);
    }
}
// or: ClassInjector.RegisterTypeInIl2Cpp<MyType>() in OnLateInitializeMelon
```

### Casting
```csharp
obj.TryCast<TargetType>()  // safe, returns null on mismatch
obj.Cast<TargetType>()     // throws on mismatch (use only for guaranteed types)
```

### Virtual Override Safety
Wrap ALL virtual overrides called by Il2Cpp in try/catch — unhandled exceptions crash the process.
```csharp
public override bool DoesItemMatchFilter(ItemInstance instance) {
    try { /* logic */ return result; }
    catch (Exception ex) { MelonLogger.Warning(ex.Message); return true; }
}
```

### Enum Fields in Il2Cpp MonoBehaviours
```csharp
// WRONG — Il2CppInterop scans and warns
private MyEnum myOption = MyEnum.Default;
// CORRECT — use int backing
private int myOption = (int)MyEnum.Default;
if ((MyEnum)this.myOption == MyEnum.Vanilla) { }
```

### Harmony WrapSafe
`[HarmonyWrapSafe]` silently swallows ALL exceptions. Always add explicit try/catch before it.

### Harmony Finalizer + Il2Cpp
Native Il2Cpp NullRefs are `Il2CppException`, NOT `System.NullReferenceException`:
```csharp
if (__exception is NullReferenceException) return null;
if (__exception is Il2CppException &&
    __exception.Message.IndexOf("NullReferenceException") >= 0) return null;
```

### FishNet Serialization
- `NetworkBehaviour` references serialized by `NetworkObject.ObjectId`, NOT GUID
- Non-networked objects serialize as null through RPCs → use `network: false` in `ObjectField.SetObject`

---

## 5. SAFETY RULES

### Null Checks on Il2Cpp Proxies
```csharp
if (obj != null && obj.gameObject != null) { /* safe to access */ }
```
Unity `Destroy()` nulls the native object — the managed Il2Cpp proxy lives on. Property access on destroyed proxy = native NullRef = process crash.

### Scene Reload Safety
- Null ALL static references in `Cleanup()`
- Clear static lists
- Deregister from GUIDManager in both `Cleanup()` AND `Destroy()`
- `Cleanup()` must be idempotent (may fire multiple times)

### UI Lifecycle
```csharp
// CORRECT: Destroy THEN DetachChildren
Destroy(child); container.DetachChildren();
// CORRECT: symmetric SetActive
warningGO.SetActive(items.Count == 0);
```

### Save/Load 4-Layer Protection
1. Per-entity try/catch — one fault doesn't kill all
2. Per-field error boundary — optional fields isolated
3. Empty-Data Guard — if Count>0 and saved==0 → return null
4. Explicit try/catch in WriteData — BEFORE `[HarmonyWrapSafe]`

### MelonLoader Logs
```powershell
# MelonLoader log
Get-Content "...\MelonLoader\Latest.log" -Tail 150
# Unity Player.log (silent Il2Cpp crashes)
Get-Content "$env:APPDATA\..\LocalLow\TVGS\Schedule I\Player.log" -Tail 150
```

---

## 6. DATA REFERENCES

### Product IDs
`ogkush`, `sourdiesel`, `greencrack`, `granddaddypurple`, `cocaine`, `meth`, `shrooms`, `liquid_meth`

### Packaging IDs
`baggies` (qty 1), `jar` (qty 5), `brick` (qty 20)

### NPC IDs
`uncle_nelson`, `billy`, `dan`, `jen`, `ming`, `oscar`, `sam`, `thomas_benzies`, `greg_fliggle`, `shirley_watts`, `fungal_phil`, `salvador`

### Property Codes
`bungalow`, `manor`, `motel_room`, `rv`, `west_storage`, `sewer_office`, `sweatshop`

### Console Commands
`settime HHMM`, `give id [qty]`, `changecash N`, `sethealth N`, `teleport x y z`, `spawnvehicle code`, `addxp N`, `setwanted N`, `growplants`, `unlockall`

### ERank (0-10)
`Street_Rat`, `Hoodlum`, `Peddler`, `Hustler`, `Bagman`, `Enforcer`, `Shot_Caller`, `Block_Boss`, `Underlord`, `Baron`, `Kingpin`

### EDay (0-6)
`Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`

### EClothingSlot (0-9)
`Feet`, `Bottom`, `Waist`, `Top`, `Outerwear`, `Hands`, `Neck`, `Eyes`, `Head`, `Wrist`

### EClothingColor (0-26)
27 colors: `White` → `Black` → `Red` → `Blue` → `Purple` → `HotPink`

---

## 7. COMMUNITY APIS

### S1API (ifBars fork, v3.0.5)
- **NuGet**: `S1API.Forked` | **Install**: `Plugins/` folder
- **Namespaces**: `S1API.PhoneApp`, `S1API.UI`, `S1API.Saveables`, `S1API.Entities`
- **Auto-discovered**: `PhoneApp` subclasses, `Saveable` subclasses
- **Phone App**:
  ```csharp
  public class MyApp : PhoneApp {
      protected override string AppName => "MyApp";
      protected override void OnCreatedUI(GameObject container) {
          var panel = UIFactory.Panel("Main", container.transform, Color.grey, fullAnchor: true);
      }
  }
  ```
- **Save System**: `[SaveableField("key")]` on fields of classes inheriting `Saveable`
- **Custom NPCs**: `S1API.Entities.NPC` with prefab builder, schedules, dialogue, customer behavior
- **UI Factory**: `Panel()`, `Text()`, `ScrollableVerticalList()`, `RoundedButtonWithLabel()`, `ButtonRow()`
- **Phone Calls**: `PhoneCallDefinition` + `CallManager.QueueCall()`

### S1MAPI (ifBars, v2.0.0)
- **Install**: `UserLibs/` folder | No game assembly dependency
- **Procedural Meshes**: `ProceduralMeshBuilder.AddBox().AddSphere().SetColor().Build()`
- **Buildings**: `BuildingBuilder.WithConfig(BuildingConfig.Medium).AddFloor().AddWalls().Build()`
- **GLTF Loading**: `GltfLoader.LoadGlb(bytes)` / `GltfImporter.SetEmissionIntensity().Load()`
- **Materials**: `MaterialPresets.Opaque()`, `.Glass()`, `.Metal()`, `.Emissive()`

---

## 8. KEY GOTCHAS

| Issue | Solution |
|-------|----------|
| ESC key opens pause menu when app closes | Set `GameInput.TogglePauseInputUsed = true` in OnEscape prefix; patch both `OnEscape` AND `OnTogglePauseMenu` |
| `Player.Inventory` only returns hotbar slots | Use `PlayerInventory.Instance.GetAllInventorySlots()` |
| `AddComponent<BuildableItem>()` crashes | Use `Instantiate(prefab)` instead |
| FishNet RPC nullifies non-networked refs | Use `network: false` in ObjectField.SetObject |
| `[HarmonyWrapSafe]` swallows exceptions silently | Add explicit try/catch before the attribute |
| Il2Cpp string marshaling in per-frame loops | Cache classification results by GetInstanceID(), not string name |
| `RawImage.color` > 1 doesn't brighten | LDR clamps at 1.0 — use overlay with inverse alpha instead |
| Premature GUID lookup returns null | Defer GUID resolution until custom location exists in scene |
| S1API mugshot overwrites custom icon | Polling enforcer re-sets sprite every frame until stable |

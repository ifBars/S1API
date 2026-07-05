---
title: "Safety Concept"
description: "Comprehensive safety layers for Schedule I MelonLoader mods — every rule based on a real bug or crash"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "All MelonLoader mods targeting Schedule I (Unity Il2Cpp)"
---

# Safety Concept: Schedule I MelonLoader Mods

!!! info
    Comprehensive reference document for all safety layers. Applies to all MelonLoader mods targeting Schedule I (Unity Il2Cpp). Every rule is based on a real bug or crash from development history.

## 1. Il2Cpp Runtime Safety

### 1.1 Null Checks on Il2Cpp Proxy Objects

| Check | Why |
|-------|-----|
| `obj != null` | Managed reference exists |
| `obj.gameObject != null` | Native Unity object not destroyed |
| Check both | Proxy can have valid managed reference but destroyed native backing |

ALWAYS check both when the object persists between frames. Unity `Destroy()` sets the native object to null, but the managed Il2Cpp proxy lives on. Property access on a destroyed proxy throws a native NullRef that crashes the process.

### 1.2 TryCast vs Cast

| Method | Behaviour on Mismatch | Use When |
|--------|------------------------|----------|
| `Cast<T>()` | `InvalidCastException` | Type is GUARANTEED (e.g., `IGUIDRegisterable`) |
| `TryCast<T>()` | Returns `null` | Type is UNCERTAIN (e.g., `ItemInstance` → `ProductItemInstance`) |

Default is `TryCast`. Use `Cast` only for interface casts or guaranteed types.
Item hierarchy: `ItemInstance` → `QualityItemInstance` → `ProductItemInstance`. A `QualityItemInstance` is NOT necessarily a `ProductItemInstance`.

### 1.3 Virtual Override Safety

Every virtual method called by Il2Cpp (e.g., `DoesItemMatchFilter`):

```csharp
public override bool DoesItemMatchFilter(ItemInstance instance)
{
    try
    {
        // Logic
        return result;
    }
    catch (Exception ex)
    {
        MelonLogger.Warning("Filter error: " + ex.Message);
        return true; // Permissive fallback
    }
}
```

Unhandled exceptions propagate through the Il2Cpp trampoline → process crash. ALWAYS use permissive fallback (`return true` for filters, no-op for actions). Restrictive fallback can block game systems.

### 1.4 Il2Cpp Type Registration

```csharp
// In OnLateInitializeMelon (NOT OnInitializeMelon)
ClassInjector.RegisterTypeInIl2Cpp<MyType>();
```

- IntPtr constructor REQUIRED: `public MyType(IntPtr ptr) : base(ptr) { }`
- DerivedConstructor REQUIRED: `base(ClassInjector.DerivedConstructorPointer<MyType>())` + `DerivedConstructorBody(this)`
- One instance per consumer (shared instances risk dangling pointers)

### 1.5 LINQ and Il2Cpp Collections

| Problem | Solution |
|---------|----------|
| `Where(s => s != null)` doesn't catch destroyed proxies | Wrap property access in try/catch |
| `OrderBy(s => s.GetPosition())` crashes on destroyed proxy | Wrap entire sort chain in try/catch with unsorted fallback |
| LINQ `orderby b.field` reads field, not return value | `Evaluate()` must set `this.field = ComputeValue()` |

### 1.6 Harmony Finalizer with Il2Cpp Exceptions

```csharp
// Native Il2Cpp NullRefs are NOT System.NullReferenceException
if (__exception is NullReferenceException)
    return null;
if (__exception is Il2CppException &&
    __exception.Message.IndexOf("NullReferenceException", StringComparison.Ordinal) >= 0)
    return null;
```

### 1.7 FishNet NetworkBehaviour Serialization

- FishNet serializes `NetworkBehaviour` via `NetworkObject.ObjectId`, NOT GUID
- Non-networked objects → `null` in RPC roundtrip
- Mod-created entities without `NetworkObject`: use `network=false` in `ObjectField.SetObject`
- GUIDManager registration still needed (Save/Load uses GUIDs)

## 2. Unity Engine Safety

### 2.1 Scene Reload

| Rule | Why |
|------|-----|
| Null all static references to GameObjects/Components in `Cleanup()` | Unity destroys all GameObjects on scene change |
| `Cleanup()` must be idempotent | Can fire multiple times (3x on exit observed) |
| Clear static lists (`Stations`, `drones`, etc.) | Old entries reference destroyed objects |
| De-register from GUIDManager in `Cleanup()` AND `Destroy()` | Prevents stale GUID entries |

### 2.2 Camera Management

| Rule | Why |
|------|-----|
| Use camera `enabled` instead of manual `Render()` | Unity auto-rendering is more reliable + consistent |
| Assign `targetTexture` BEFORE `enabled = true` | Otherwise camera renders to screen |
| `SetCameraActive(false)` on App-Close/Tab-Switch | No running cameras in background |
| `useOcclusionCulling = false` for mod cameras | Mod cameras have no baked occlusion context |
| Copy `CullingMask` from `Camera.main`, not `-1` | Prevents mod artifacts on excluded layers |

### 2.3 RenderTexture Lifecycle

```
EnterFullscreen:  Create RT → assign to camera → enabled=true
ExitFullscreen:   enabled=false → restore original RT → release fullscreen RT
```

- Fullscreen RT at `Screen.width × Screen.height` (native resolution)
- ALWAYS release (`Release()`) on exit — otherwise GPU memory leak
- Standalone RTs (`new RenderTexture()`) are NOT part of the scene hierarchy and are NOT automatically destroyed on scene change. Explicit `Release()` + `Destroy()` in `OnSceneWasLoaded()` or `Cleanup()` path is REQUIRED.
- Cleanup methods for RTs must be double-call-safe (null-check after Destroy, null assignment). `OnDestroy()` on Il2Cpp-injected types does NOT fire reliably — therefore ALWAYS add an explicit cleanup call in the scene transition path.

### 2.4 UI GameObject Lifecycle

| Operation | Correct | Wrong |
|-----------|---------|-------|
| Empty container | `Destroy()` THEN `DetachChildren()` | Only `DetachChildren()` → orphaned GameObjects |
| Close modal dialog | `Destroy(overlayGO)` | `SetActive(false)` → accumulates |
| Refresh ScrollRect | Destroy children, recreate | Recycle children (fragile with dynamic content) |
| Warning-GO visibility | `SetActive(items.Count == 0)` symmetric | Only `SetActive(false)` → never visible again |

### 2.5 Physics

| Rule | Why |
|------|-----|
| `CollisionDetectionMode.ContinuousDynamic` for fast objects | `Discrete` → tunneling through thin walls |
| Reset `ascendBeforeHorizontal` on manual control | Otherwise `excludeLayers = ~0` → no collisions |
| 5m radius check before vertical liftoff | Prevents liftoff triggering after job completion (drone already removed) |

### 2.6 MonoBehaviour Lifecycle

- `Update()` does NOT run when GameObject is inactive → logic that must always run belongs in `DroneApp.Update()` (always active)
- `OnDestroy()` fires on scene unload for ALL GameObjects → null-guards required
- `GetComponent<T>()` returns null when component missing → ALWAYS check if not guaranteed

### 2.7 Unity LDR Rendering — Color Multiplier Limitation (CHANGE-032)

`RawImage.color` (and any UI color multiplier) can ONLY darken/tint LDR content, NEVER brighten. Values >1 have no visible effect on standard RenderTextures.

| Misconception | Reality |
|---------------|---------|
| `RawImage.color = new Color(1.5f, 1.5f, 1.5f)` brightens | No effect — LDR clamps at 1.0 |
| Apply Image Corrections (Brightness/Gamma) to feed tint | No visible effect — Tint is a multiplier |
| Overlay layers don't matter | Overlay ABOVE the feed dominates visual output |

**Correct architecture for visual effects over LDR feeds:**

```
Feed tint:    Neutral (1,1,1,1) — no corrections on the feed
Overlay:      Separate Image ABOVE the feed, own color/alpha
Corrections:  Apply to overlay, NOT to feed
              → Brightness controls overlay alpha inversely (alpha = base / brightness)
              → Brighter brightness = less overlay = brighter image
```

## 3. MelonLoader / Harmony Safety

### 3.1 [HarmonyWrapSafe] Trap

`[HarmonyWrapSafe]` SILENTLY swallows ALL exceptions. No log, no error, just gone.

**Mitigation**: Explicit try/catch BEFORE `[HarmonyWrapSafe]`:

```csharp
[HarmonyPostfix]
[HarmonyWrapSafe]
public static void WriteData(Player __instance, string path)
{
    try
    {
        // Entire logic
    }
    catch (Exception ex)
    {
        MelonLogger.Error("Save failed: " + ex.ToString());
    }
}
```

### 3.2 Harmony Patch Best Practices

| Rule | Why |
|------|-----|
| Prefix: use `return false` with caution | Blocks ALL postfixes + original method |
| Postfix: reference parameters with `ref` | Otherwise output cannot be modified |
| Finalizer: check BOTH exception types | `NullReferenceException` + `Il2CppException` |
| ALWAYS patch Unity lifecycle entry points | Il2Cpp native-to-native calls bypass managed Harmony |

### 3.3 Which Methods to Patch

| Yes | No |
|-----|-----|
| `Update()`, `LateUpdate()`, `FixedUpdate()` | Internal helpers called from native code |
| `Awake()`, `Start()`, `OnDestroy()` | Private methods only called by other native methods |
| Public API methods with managed callers | `EnableImpostor()`/`DisableImpostor()` (native LateUpdate bypasses patch) |

### 3.4 MelonMod Lifecycle

```
OnInitializeMelon     → Settings, Preferences
OnLateInitializeMelon → Il2Cpp Type Registration
OnSceneWasLoaded      → Init on Main Scene, Cleanup on Others
OnUpdate              → Frame-based logic (e.g., wait for AppIcons before DroneApp creation)
OnLateUpdate          → Post-frame updates (e.g., Camera.main repositioning)
```

### 3.5 Diagnostic Logging Pattern

```csharp
// Every Harmony patch gets a tag
MelonLogger.Msg("[PATCH] Player.Awake fired");
MelonLogger.Msg("[SAVE] WriteData fired — Stations: " + count);
MelonLogger.Msg("[LOAD] Save file loaded (" + chars + " chars)");
```

**Diagnosis workflow**: If `[PATCH]`/`[SAVE]`/`[LOAD]` are completely missing → Harmony patches are not applying (MelonLoader version, game update, mod conflict).

### 3.6 UserLibs Deployment

External managed DLLs (e.g., NLayer) are deployed as UserLib in MelonLoader's `UserLibs/` folder. MelonLoader loads these automatically on startup — no `AssemblyResolve` handler needed.

| Rule | Why |
|------|-----|
| Place DLL in `UserLibs/`, NOT as EmbeddedResource | Fragile, version conflicts in multi-mod, invisible in filesystem |
| PostBuildEvent: Mod DLL → `Mods/`, external DLLs → `UserLibs/` | Automation prevents forgotten deploys |
| No `AssemblyResolve` handler | MelonLoader handles loading automatically |

#### DLL Naming Convention

| Situation | Rule | Example |
|-----------|------|---------|
| DLL can be renamed, **no** cross-mod sharing | Rename to mod name: `<ModName>.<LibName>.dll` | `Drones.AudioLib.dll`, `Rave.Utils.dll` |
| DLL **cannot** be renamed (signed, strong reference) | Keep original name | `NLayer.dll` |
| DLL is **shared by multiple mods** | Use neutral prefix: `S1Shared.<LibName>.dll` | `S1Shared.AudioLib.dll` |

## 4. Save/Load Data Integrity

### 4.1 Four-Layer Protection

```
Layer 1: Per-Entity try/catch
  → One faulty entity doesn't kill the entire save
  → Counts saved/skipped with Warning log

Layer 2: Per-Field Error Boundary
  → Optional fields (e.g., active transfer jobs) in own try/catch
  → Core data survives even if optional data fails

Layer 3: Empty-Data Guard
  → If entities in memory (Count > 0) but 0 saved → return null
  → Prevents overwriting valid save file with empty JSON

Layer 4: Explicit try/catch in WriteData
  → Catches AND logs errors BEFORE [HarmonyWrapSafe] swallows them
  → Null-check on saveString consumes Empty-Data Guard
```

### 4.2 Load Mitigation

```
Per-Station try/catch + continue on error
  → One corrupt station is skipped, rest loads normally
  → CRITICAL: null-cfg MUST have continue, not just logging
  → Without continue: FindProperty(null) crashes → entire load lost

Per-Drone try/catch + continue on error
  → Same logic as Stations

Guard against Double-Load
  → if (Stations.Count > 0) return
  → LoadString is called 2× (~4s apart), 2nd call is harmless
```

### 4.3 Save Trigger Understanding

`Player.WriteData()` fires ONLY on:
- Sleep / day transition
- Quick Save Mod
- Save & Exit

No automatic periodic saves. Short sessions without save event = 0 writes = normal game behaviour.

### 4.4 skipNextSave Lifecycle

```
Cleanup()           → skipNextSave = true  (Scene transition, don't save)
CacheSaveData()     → skipNextSave = false (Save loaded, resume saving)
OnSceneWasLoaded()  → skipNextSave = false (Fallback for new games without save)
```

Without the fallback reset: New games (no `Drones.json` → no `CacheSaveData()`) can never save.

### 4.5 SetStoredItem vs HardFilters

- `SetStoredItem()` is a direct setter → bypasses HardFilters → Save/Load-safe
- `GetCapacityForItem()` / `GetInputCapacityForItem()` check HardFilters → Employee routing
- Intentional: Saved items must be loadable even if filters changed

## 5. Schedule I Game-Specific Safety

### 5.1 Property / Entity System

| Rule | Why |
|------|-----|
| `PlaceableStorageEntity` as base for stations | Compatible with game routing (Employees, Clipboard, Destinations) |
| `EntityConfiguration.Name` is the authoritative source | Clipboard rename only changes this, not `StorageEntityName` |
| Manual sync required (`SyncNameFromEntityConfiguration`) | Game only syncs within its own systems |
| Cache `cachedPSE`, don't call `GetComponent` every frame | Performance + null-safe on destroyed object |

### 5.2 Employee / Handler Routing

The game's employee system uses:
1. `GetInputCapacityForItem()` → checks `DoesItemMatchHardFilters()`
2. Destination-Picker → checks `DestinationFilter`

Stations without HardFilter accept everything from employees (intentional after CHANGE-001).

### 5.3 Clipboard / WorldspaceCanvas

| Problem | Solution |
|---------|----------|
| Clipboard text sticks after looking away | Manual clearing in `Update` postfix |
| `ManagementWorldspaceCanvas.UpdateUIs` NullRef | Filter Drone Station PSEs from `ShownConfigurables`/`SelectedConfigurables` |
| Don't null `HoveredConfigurable`/`OutlinedConfigurable` | Vanilla `UpdateSelection()` needs them for outline transitions |
| SyncVar operations no-op on Drone Stations | No `NetworkObject` → FishNet calls ignored |

### 5.4 Distance-Based Game Systems

The following systems use `Camera.main` position:
- `UnluckDistanceDisabler` (LOD / Object Streaming)
- `AvatarImpostor` (NPC Billboard vs 3D)
- Occlusion Culling
- LODGroup

Mod cameras at different positions see an "empty world" if `Camera.main` is elsewhere. Move `Camera.main` to mod camera position (CHANGE-023).

## 6. Mod Compatibility

### 6.1 Non-Destructive Integration

| Principle | Implementation |
|-----------|----------------|
| Prefer vanilla lists | `IsCustomNPC` for S1API NPCs, not custom NPC lists |
| No UI tabs for third-party mods | Automatic detection via game APIs |
| Reflection + try/catch for optional integration | If foreign mod not loaded → graceful degrade |
| `FindObjectOfType` instead of static references | Works regardless of load order |

### 6.2 StackLimit Mod Compatibility

- Use `GetCapacityForItem()` instead of `slot.Quantity < slot.MaxQuantity`
- Respects dynamic stack limits from third-party mods

### 6.3 Destination Picker Compatibility

Harmony Postfixes on `PackagingStationConfiguration.DestinationFilter` and `BrickPressConfiguration.DestinationFilter` allow Drone Stations as targets in game destination pickers.

### 6.4 Harmony Patch Conflicts

| Risk | Detection | Mitigation |
|------|-----------|------------|
| Another mod patches the same method | `[PATCH]` tags missing in logs | Diagnostics logging (CHANGE-025) |
| Prefix `return false` blocks our Postfix | Feature doesn't work | Use Prefix instead of Postfix when critical |
| Transpiler modifies IL structure | Unpredictable crashes | Don't use Transpilers, prefer Prefix/Postfix |

### 6.5 S1API Compatibility

S1API is the most-used mod framework for Schedule I. It modifies three game systems that the Drones mod uses. **Goal: Drones mod has NO dependency on S1API, but must be defensive against it.**

#### Phone App System

| S1API Intervention | Impact on Drones | Defense |
|-------------------|------------------|---------|
| `ProductManagerAppStartPatch`: Blocks `Start()` on cloned ProductManagerApp (name ≠ "ProductManagerApp") | Clone's internal fields stay null → `App<T>.SetOpen()` throws | Don't use `App<T>.SetOpen()`. Manage `Phone.ActiveApp` directly (CHANGE-053) |
| `HomeScreenScrollPatch`: Creates second "AppIcons" GO as stub | `GameObject.Find("AppIcons")` may find stub | Clone icon into found AppIcons — S1API's `AppIconsRedirect` moves it to real container automatically |
| `ExitListener` system instead of Harmony | Drones Harmony Prefix on `GameInput.Exit` blocks original → ExitListeners never fire | No conflict — S1API listeners check `IsOpen()` before action |

**Pattern for Phone App Mods (CHANGE-053 Rev.2)**:

Do NOT use ProductManagerApp clone! `Instantiate(productManagerApp)` triggers `App<T>.Awake()` → singleton corruption → all other apps broken.

```csharp
// Build panel from scratch:
canvas = new GameObject("MyModApp");
canvas.transform.SetParent(appsCanvas.transform, false);
// Create Container + Background manually...

// Open:
Phone.ActiveApp = canvas;
container.gameObject.SetActive(true);
PlayerSingleton<AppsCanvas>.instance.SetIsOpen(true);
PlayerSingleton<HomeScreen>.instance.SetIsOpen(false);
phone.SetIsHorizontal(true);  // for Landscape

// Close:
container.gameObject.SetActive(false);
Phone.ActiveApp = null;
phone.SetIsHorizontal(false);
phone.SetLookOffsetMultiplier(1f);
phone.RequestCloseApp();

// Icon: Clone + replace onClick (don't hijack last icon)
iconButton.onClick.RemoveAllListeners();
iconButton.onClick.AddListener(() => OpenApp());
```

#### Storage System

| S1API Patch | Fires on DroneStation? | Risk |
|-------------|------------------------|------|
| `PlaceableStorageEntity.Start` (Postfix, Event-Raising) | YES (ItemInstance.Definition != null) | LOW — only event, no slot modification |
| `PlaceableStorageEntityLoader.Load` (Prefix return false) | NO — own Save/Load path | NONE |
| `ItemSet.LoadTo` / `BuildableItem.GetSaveData` | NO / only additive | NONE |

CHANGE-035 Slot-Truncation (`while (ItemSlots.Count > 12) RemoveAt`) runs AFTER `Start()` → also protects against potential S1API event subscribers.

#### Clipboard / ManagementWorldspaceCanvas

S1API does NOT patch `ManagementWorldspaceCanvas` or Clipboard methods. Drones patches are completely unaffected.

## 7. UI / Phone App Safety

### 7.1 Tab Switch Safety

```csharp
// OnTabChanged → ShowScreen → RefreshData
// ALWAYS null-check screen instance
if (this.inventoryScreen != null)
{
    this.inventoryScreen.RefreshInventory();
}
```

### 7.2 Refresh Pattern for List UIs

```csharp
public void RefreshCards()
{
    if (this.contentRT == null) return;
    try
    {
        // 1. DESTROY old children (don't just detach!)
        for (int c = this.contentRT.childCount - 1; c >= 0; c--)
            Object.Destroy(this.contentRT.GetChild(c).gameObject);
        this.contentRT.DetachChildren();

        // 2. Defensive snapshot copy of data
        List<T> snapshot = source != null
            ? new List<T>(source)
            : new List<T>();

        // 3. Toggle warning GO symmetrically
        if (this.noItemsGO != null)
            this.noItemsGO.SetActive(snapshot.Count == 0);

        // 4. Per-item try/catch
        foreach (T item in snapshot)
        {
            try { CreateCard(item); }
            catch (Exception ex) { MelonLogger.Warning("Card error: " + ex.Message); }
        }
    }
    catch (Exception ex)
    {
        MelonLogger.Warning("Refresh error: " + ex.Message);
    }
}
```

### 7.3 Fullscreen Mode Lifecycle

```
Enter:
  1. Create screen-resolution RT
  2. Copy CullingMask from Camera.main
  3. Disable PlayerCamera
  4. Null Camera.main cullingMask
  5. Configure + enable drone camera
  6. Enable overlay Canvas
  7. Set activeFullscreenDrone

Exit (ALL steps in reverse order):
  1. Nightvision OFF
  2. Disable overlay Canvas
  3. Re-enable PlayerCamera
  4. Restore Camera.main cullingMask
  5. Restore drone camera: original RT, cullingMask, occlusionCulling
  6. Release fullscreen RT
  7. activeFullscreenDrone = null
```

`ExitFullscreen()` must restore EVERY field that `EnterFullscreen()` changed.

### 7.4 Manual Control Cleanup

`UnselectCard()` is the central cleanup point:
- Disable manual mode
- Make cursor visible again
- Nightvision OFF
- isManualModeActive = false
- Hide all hint labels

Called from: App-Close, Tab-Switch, Drone Sale, Card Change.

### 7.5 Auto-Refresh Timer

```csharp
// Only for the active tab, not globally
if (flag && DroneApp.currentScreen == "Inventory" && this.inventoryScreen != null)
{
    this.inventoryRefreshTimer += Time.deltaTime;
    if (this.inventoryRefreshTimer >= InventoryRefreshInterval)
    {
        this.inventoryScreen.RefreshInventory();
        this.inventoryRefreshTimer = 0f;
    }
}
```

### 7.6 Phone App Open/Close Lifecycle (CHANGE-053 Rev.2)

Do NOT clone ProductManagerApp! `Instantiate(productManagerApp)` triggers `App<T>.Awake()` → singleton corruption → all other phone apps broken. Build panel from scratch.

```
Create App:
  1. Create new GO "DroneApp" under AppsCanvas
  2. Create Container GO + Background GO manually
  3. NO App<T> component — no Awake/Start/Update problem
  4. Clone icon (first child of AppIcons as template)
  5. Replace icon onClick → own OpenApp() method
  6. Deactivate container at end of Setup()

Open:
  1. If other ActiveApp open → RequestCloseApp()
  2. Phone.ActiveApp = canvas
  3. Activate container (SetActive true)
  4. AppsCanvas.SetIsOpen(true) — shows app area
  5. HomeScreen.SetIsOpen(false) — hides home
  6. Phone.SetIsHorizontal(true) — landscape mode
  7. Phone.SetLookOffsetMultiplier(0.6f)

Close (S1API SetAppOpen(false) order — CRITICAL):
  1. AppsCanvas.SetIsOpen(false) — hides app area
  2. HomeScreen.SetIsOpen(true) — shows home
  3. Phone.ActiveApp = null
  4. Phone.SetIsHorizontal(false)
  5. Phone.SetLookOffsetMultiplier(1f)
  6. Deactivate container (SetActive false)
  IMPORTANT: Harmony Prefix must return false (≡ S1API exit.Used=true).
  Reason: SetIsHorizontal starts a coroutine. If game's Exit() calls
  SetIsOpen(false) in the SAME frame, transitions collide →
  Phone closes visually but movement stays locked.
  Two-Step: ESC 1 = App closes (HomeScreen visible),
            ESC 2 / Tab = Phone closes (Player can move).

Update (per frame):
  1. flag = phone.IsOpen && Phone.ActiveApp == this.canvas
  2. active→inactive Transition: Deactivate container + reset orientation
     (covers Phone-Close without explicit CloseApp)
```

Why clone icon instead of hijacking: The last child of AppIcons belongs to another app — its onClick points to the wrong `ShortcutClicked()`. Cloning + replacing onClick is the only safe way.

## 8. Meta: Securing the Safeguards

### 8.1 Diagnostics That Prove Safeguards Are Working

| Safeguard | Proof Mechanism |
|-----------|-----------------|
| Harmony patches active | `[PATCH] Player.Awake fired` in Latest.log |
| Save path intact | `[SAVE] Successfully saved X stations, Y drones (Z chars)` |
| Load path intact | `[LOAD] Successfully loaded X stations, Y drones` |
| Per-Entity isolation | `stationsSkipped`/`dronesSkipped` counters with Warning |
| Empty-Data Guard | `[SAVE] Save skipped: data integrity protection` |
| Scene Cleanup | `Cleanup()` log on scene transition |

### 8.2 Checklist for New Features

Before every merge:

- [ ] **Null-Guards**: Every Il2Cpp property access null-checked?
- [ ] **TryCast**: No `Cast<T>()` on uncertain types?
- [ ] **Scene Reload**: Static references cleaned in `Cleanup()`?
- [ ] **Save Impact**: New fields in `GetSaveData()`/`LoadSaveData()`? Per-field try/catch?
- [ ] **UI Lifecycle**: `Destroy()` before `DetachChildren()`? Warning-GOs symmetric?
- [ ] **Camera Cleanup**: `SetCameraActive(false)` on App-Close/Tab-Switch?
- [ ] **Manual Mode**: New UI element reset in `UnselectCard()`?
- [ ] **Fullscreen**: New field in `EnterFullscreen()`? Also restored in `ExitFullscreen()`?
- [ ] **Error Isolation**: try/catch when iterating over collections?
- [ ] **Fallback**: Permissive fallback on error (not blocking)?
- [ ] **Logging**: Warning/Error on skip/error (not silent swallow)?
- [ ] **Mod Compatibility**: `FindObjectOfType` instead of static refs? `GetCapacityForItem()`?
- [ ] **S1API Compatibility**: Phone app via `Phone.ActiveApp` (not `SetOpen`)? Storage clone with slot truncation? No `App<T>.SetOpen()` call?

### 8.3 Checklist for Save/Load Changes

- [ ] `GetSaveData()` has null-check on `gameObj`?
- [ ] `GetSaveData()` has try/catch around optional fields (Jobs, Items)?
- [ ] `LoadSaveData()` has per-entity try/catch with `continue`?
- [ ] Null-cfg/null-property → `continue`, NOT just log?
- [ ] `GetSaveString()` counts saved/skipped?
- [ ] Empty-Data Guard (Count > 0 && saved == 0 → return null)?
- [ ] `WriteData` has explicit try/catch BEFORE `[HarmonyWrapSafe]`?
- [ ] `skipNextSave` correctly set/reset in all paths?

### 8.4 Checklist for Harmony Patches

- [ ] Lifecycle entry point patched (Update/LateUpdate), not internal helper?
- [ ] `[HarmonyWrapSafe]` → explicit try/catch before it?
- [ ] Finalizer checks BOTH exception types?
- [ ] Diagnostic `[TAG]` logging present?
- [ ] Prefix `return false` only when absolutely necessary?

### 8.5 Post-Fix Validation

After every fix:
1. `run_build` → compiles?
2. User tests in-game → works?
3. Check both logs (Latest.log + Player.log) → no new errors?
4. Update documentation (project-context.md / il2cpp-rules.md)

### 8.6 Log Inspection After Runtime Error

```powershell
# MelonLoader (Mod-Level)
Get-Content "C:\Steam\steamapps\common\Schedule I\MelonLoader\Latest.log" -Tail 150

# Unity Player.log (Silent Errors)
Get-Content "$env:APPDATA\..\LocalLow\TVGS\Schedule I\Player.log" -Tail 150

# Targeted search
Select-String -Path "...\Latest.log" -Pattern "Exception|NullRef|ERROR"
Select-String -Path "...\Player.log" -Pattern "Exception|NullRef|Drones"
```

Always check both logs — Player.log contains silent Il2Cpp crashes that don't appear in Latest.log.

## 9. Anti-Patterns (What NOT to do)

| Anti-Pattern | Why Bad | Correct Pattern |
|-------------|---------|-----------------|
| `catch { }` (empty catch) | Hides real bugs | `catch (Exception ex) { MelonLogger.Warning(ex.Message); }` |
| `[HarmonyWrapSafe]` without own try/catch | Silently swallows errors | Own try/catch + log BEFORE it |
| `DetachChildren()` without `Destroy()` | Memory leak (orphaned GOs) | First `Destroy()`, then `DetachChildren()` |
| `Cast<T>()` on uncertain types | `InvalidCastException` → crash | `TryCast<T>()` + null-check |
| Null logging but not skipping (`continue`) | Code continues with null → crash | `if (x == null) { Log; continue; }` |
| Static references without cleanup | Stale after scene reload → crash | Null or clear list in `Cleanup()` |
| `CollisionDetectionMode.Discrete` | Fast objects tunnel through walls | `ContinuousDynamic` |
| `noItemsGO.SetActive(false)` without counterpart | Never visible again | `SetActive(items.Count == 0)` symmetric |
| `Camera.main.cullingMask = -1` | Renders ALL layers including mod artifacts | Copy `cullingMask` from `Camera.main` |
| Patching internal Il2Cpp helpers | Native-to-native calls bypass Harmony | Patch lifecycle entry points |
| Not resetting `excludeLayers = ~0` | All collisions disabled | Reset on state change (e.g., Manual Control) |
| Entire save list without per-entity try/catch | One error → everything lost | Per-entity try/catch + continue |
| `RawImage.color` >1 for brightening | LDR clamps at 1.0 → no effect | Overlay layer with inverse alpha control (CHANGE-032) |
| Corrections on feed tint instead of overlay | Feed tint is multiplier → can only darken | Apply corrections to overlay, keep feed neutral |

## 10. Performance Optimization at Unity/Il2Cpp Level

!!! tip
    Performance bottlenecks in MelonLoader mods are **not** at the NuGet package level, but at the Unity/Il2Cpp level. Every technique here addresses hot paths that execute per frame or per UI refresh.

### 10.1 Techniques Overview

| # | Technique | Priority | When to Apply |
|---|-----------|----------|---------------|
| 1 | Cache `GetComponent<T>()` instead of calling per frame | High | Every component needed in Update/FixedUpdate |
| 2 | Use `StringBuilder` instead of string concat in hot paths | Medium | Log statements with `+` in `Update()` / `FixedUpdate()` |
| 3 | Use `StringComparison.Ordinal` instead of default for string comparisons | Medium | All `Contains()`, `IndexOf()`, `Equals()` on strings |
| 4 | Use `HashSet<T>` instead of `List<T>` for lookup operations | Medium | `Contains()` checks on lists with >5 elements |
| 5 | Object pooling for frequently created/destroyed objects | Low | UI elements (Drone Cards), temporary GameObjects |
| 6 | Pre-allocate `List<T>` with Capacity | Low | Snapshot lists whose approximate size is known |

### 10.2 Implementation Plan

#### Step 1: Check GetComponent Caching (already implemented)
- `cachedPSE` in `DroneStation.cs` is the reference pattern

#### Step 2: StringBuilder in Hot Paths
- Find all `MelonLogger.Msg("text" + var + "text")` in `Update()`/`FixedUpdate()`/`LateUpdate()`
- Replace with `StringBuilder` or `$""` interpolation (interpolation is acceptable with <5 parts)

#### Step 3: Ordinal String Comparisons
- Find all `string.Contains()`, `string.Equals()`, `IndexOf()` without Comparer
- Add `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase`

#### Step 4: HashSet instead of List for Lookups
- Identify `Stations.Contains()` and similar lookup patterns
- If collection is frequently queried via `Contains()`: switch to `HashSet<T>`

#### Step 5: Object Pooling Evaluation
- Drone Cards and UI elements created/destroyed in `RefreshCards()`
- Simple pool pattern: `Queue<GameObject>` with `Get()`/`Return()` methods

#### Step 6: List Pre-allocation with Capacity
- Find snapshot lists in `RefreshCards()` and similar methods
- Use `new List<T>(expectedCount)` instead of `new List<T>()`

### 10.3 Rules for Performance Changes

| Rule | Rationale |
|------|-----------|
| Only optimize hot paths (Update, FixedUpdate, Refresh) | Init code runs once — readability matters more |
| Measure before optimizing | No premature optimization — only when frame drops are visible |
| One step per Copilot session | Avoids token budget overflow and eases rollback |
| Build + in-game test after each step | Performance changes can introduce logic bugs |
| No Il2Cpp-specific workarounds without verification | `Il2CppArrayBase` behaves differently from `List<T>` |

## See Also

- [Il2Cpp Rules & Patterns](il2cpp-rules.md) — Detailed Il2Cpp type registration and pitfalls
- [Modding Guide](modding-guide.md) — Environment setup and Harmony basics

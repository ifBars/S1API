---
title: "Il2Cpp Rules & Patterns"
description: "Essential patterns for working with Il2Cpp types, MonoBehaviours, Harmony patching, and common pitfalls"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I (Il2Cpp backend)"
---

# Il2Cpp Rules & Patterns

## Custom Il2Cpp Type Registration

### Pattern: Subclassing Il2Cpp Types (non-MonoBehaviour)

When creating managed classes that extend Il2Cpp types (e.g., `ItemFilter`, `ItemDefinition`):

```csharp
public class MyFilter : ItemFilter
{
    // Required: IntPtr constructor for Il2Cpp interop
    public MyFilter(IntPtr pointer) : base(pointer) { }

    // Required: Parameterless constructor with DerivedConstructorPointer
    public MyFilter() : base(ClassInjector.DerivedConstructorPointer<MyFilter>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    // Virtual override — called from Il2Cpp side
    public override bool DoesItemMatchFilter(ItemInstance instance) { ... }
}
```

**Registration** in `OnLateInitializeMelon`:

```csharp
ClassInjector.RegisterTypeInIl2Cpp<MyFilter>();
```

### Pattern: Il2Cpp MonoBehaviour Proxy (REQUIRED for all MonoBehaviours)

Every managed class that inherits from `MonoBehaviour` and lives as a Component on GameObjects **MUST** be registered as an Il2Cpp proxy. Without registration, `AddComponent<T>()` crashes at runtime.

**Structure:**

```csharp
public class MyProxy : MonoBehaviour
{
    // REQUIRED: IntPtr constructor for Il2Cpp interop
    public MyProxy(IntPtr ptr) : base(ptr) { }

    // Managed-only fields (not Il2Cpp-visible)
    private SomeType _field;

    // Unity lifecycle methods work normally
    private void LateUpdate() { ... }

    // Mark managed-only methods with [HideFromIl2Cpp]
    [HideFromIl2Cpp]
    internal void Initialize(...) { ... }
}
```

**Registration** in `OnInitializeMelon()` (BEFORE first `AddComponent` call):

```csharp
ClassInjector.RegisterTypeInIl2Cpp<MyProxy>();
```

**Rules:**

| Rule | Why |
|------|-----|
| IntPtr constructor is mandatory | Il2Cpp runtime instantiates via native pointer |
| `[HideFromIl2Cpp]` for managed-only methods | Prevents Il2Cpp marshaling errors on non-blittable parameters |
| Register BEFORE first `AddComponent` | Otherwise: `MissingMethodException` at runtime |
| No static fields holding scene objects | Scene reload destroys GameObjects → stale pointers |
| `LateUpdate`/`Update` must not throw exceptions | Unhandled exception in Unity callback → frame kill, no crash log |

**Enum fields in Il2Cpp MonoBehaviours (MANDATORY):**

Managed enums from non-Il2Cpp assemblies produce `Assembly X is not registered in il2cpp` warnings when Il2CppInterop scans the fields during type registration. Fix:

```csharp
// WRONG — Il2CppInterop scans the field type and finds no Il2Cpp registration
private MyEnum myOption = MyEnum.Default;

// CORRECT — int field, Il2CppInterop ignores primitive types
private int myOption = (int)MyEnum.Default;

// Access: explicit cast everywhere
if ((MyEnum)this.myOption == MyEnum.Vanilla) { ... }
this.myOption = (int)newValue;
```

Additionally: All methods (even `private`) with enum parameters or enum return types MUST use `[HideFromIl2Cpp]` — Il2CppInterop cannot marshal managed enums.

### Safety Rules for Il2Cpp Virtual Overrides

| Rule | Why |
|------|-----|
| ALWAYS wrap virtual override body in try/catch | Unhandled exceptions propagate through Il2Cpp trampoline → process crash |
| Use permissive fallback on error (`return true` for filters) | Blocking fallback can brick game systems silently |
| Log caught exceptions with `MelonLogger.Warning` | Silent swallowing hides real bugs |
| Never throw from Il2Cpp-called overrides | No managed exception handler exists on the Il2Cpp call stack |

### Instance Lifetime Rules

| Rule | Why |
|------|-----|
| Create ONE instance per consumer (e.g., per ItemSlot) | Shared instances risk dangling pointers if any consumer's cleanup disposes the object |
| `DerivedConstructorBody` registers a GCHandle internally | Prevents managed GC while Il2Cpp side holds reference |
| Don't cache Il2Cpp instances in static fields across scene reloads | Unity destroys GameObjects on scene change → stale Il2Cpp pointers |

## Scene Reload Safety Checklist

When adding new features that create Il2Cpp objects (GameObjects, Components, custom types):

1. **Cleanup path**: Verify `DroneStation.Cleanup()` / `DroneManager.Cleanup()` releases or invalidates all references
2. **Recreation path**: Verify `Load()` → `Create()` → setup methods re-apply the feature from scratch
3. **No stale static references**: Scene reload destroys all GameObjects; any static reference to a destroyed object → crash
4. **Filter/HardFilter**: Applied in `CloneStorageUnit()` which runs on every station creation — reload-safe by design

## Common Pitfalls

### S1API `QuestEntry.ClearPOI()` in constructor → CompassManager NRE Storm

!!! warning "Gotcha"
    Calling `EntryX.ClearPOI()` directly in the quest constructor after `AddEntry(...)` to "set POI manually later" causes a `NullReferenceException` in `CompassManager.UpdateElements()`.

    **What happens internally:** `ClearPOI()` calls `UpdateCompassElement()`. S1API delays POI presentation by 1 frame, and vanilla `QuestEntry.Start()` only then calls `CompassManager.AddElement(...)`. The `compassElement` is half-initialized.

    **Fix:** Remove ctor-`ClearPOI()` entirely. Either:
    - **Job Quests**: Call `Apply(...)` immediately after `QuestManager.CreateQuest<T>`.
    - **Permanent Quests**: In `EnsureStarted()`, call `RefreshPoi()` right after `CreateQuest<T>`.
    - Gate on a resolved target transform (`sqrMagnitude >= 100f`), never start with `Vector3.zero`.

### TryCast vs Cast for Il2Cpp Types

- `Cast<T>()` throws `InvalidCastException` if type doesn't match → crash in Il2Cpp context
- `TryCast<T>()` returns `null` on mismatch → safe for type checking
- ALWAYS use `TryCast` when the type is uncertain (e.g., `ItemInstance` that could be `ProductItemInstance` or something else)

!!! tip "Item Type Hierarchy"
    `ItemInstance` → `QualityItemInstance` → `ProductItemInstance`. A `QualityItemInstance` is NOT necessarily a `ProductItemInstance`. Use base class properties (`ItemInstance.ID`, `ItemInstance.Quantity`) where `ProductItemInstance`-specific properties aren't needed.

### Null Checks on Il2Cpp Properties

Il2Cpp proxy objects can have valid managed references but destroyed native objects:

- Check both the managed reference AND the underlying object: `obj != null && obj.gameObject != null`
- `AppliedPackaging` (property accessor) can return null for unpackaged products — always null-check

### SetStoredItem vs HardFilters

- `ItemSlot.SetStoredItem()` is a direct setter — does NOT check HardFilters
- `GetCapacityForItem()` / `GetInputCapacityForItem()` DO check HardFilters
- Employee/Handler system uses capacity checks → HardFilters block routing
- `LoadSaveData` uses `SetStoredItem` directly → saved items bypass filters (desired: don't corrupt saves)

### FishNet NetworkBehaviour Serialization

- FishNet serializes `NetworkBehaviour` references by `NetworkObject.ObjectId`, NOT by GUID
- Non-networked objects (no `NetworkObject` component) serialize as **null** through FishNet RPCs
- `ConfigurationReplicator.ReplicateField` uses FishNet RPCs internally
- Fix for non-networked entities: bypass replication by setting `network=false` in `ObjectField.SetObject`
- GUIDManager registration is still needed for save/load (ObjectField.Load resolves by GUID)

### GUIDManager Registration

!!! warning "Gotcha"
    `BuildableItem.SetGUID(Guid)` only sets the GUID backing field — does NOT register with GUIDManager.

    Registration normally happens in `InitializeGridItem`/`OnSpawnServer` (bypassed for mod-created entities).
    Must explicitly call `GUIDManager.RegisterObject(bi.Cast<IGUIDRegisterable>(), gameObject)` after SetGUID.
    Must deregister in `Cleanup()` and `Destroy()` to prevent stale GUID entries across scene reloads.

### AddComponent on Il2Cpp Game Components (CRITICAL)

!!! danger "Critical"
    Il2Cpp game components cannot be created from scratch via `AddComponent<T>()` if they have complex `Awake()` methods.

    - `BuildableItem` extends `GridItem` — `GridItem.Awake()` NREs on minimal GameObjects (expects full game initialization)
    - On **inactive GO**: `AddComponent<BuildableItem>()` returns **null** (FishNet `NetworkBehaviour` requires active GO)
    - On **active GO**: `AddComponent` succeeds but `GridItem.Awake()` NREs immediately (7+ NullReferenceExceptions)

    **Rule**: Never create complex Il2Cpp game components from scratch. Clone them from existing scene objects via `Instantiate<T>()`.
    Same applies to: `FootprintTile`, `TileAppearance`, `TileDetector`, `CornerObstacle`, `StorageEntity`, `PlaceableStorageEntity` etc.

### Harmony 2.x: Prefix return false Does NOT Skip Postfixes (CRITICAL)

!!! danger "Critical"
    In Harmony 2.x, a `[HarmonyPrefix]` returning `false` **skips the original method** but **all Postfixes still run**.

    This is by design — Postfixes are guaranteed to run regardless of Prefix return value.

    **Pattern**: Use `[HarmonyFinalizer]` to catch exceptions thrown by Postfixes:

    ```csharp
    [HarmonyPatch(typeof(SomeType), "SomeMethod")]
    class MyPatch
    {
        [HarmonyPrefix]
        static bool Prefix(...) => false; // Skips original, NOT postfixes

        [HarmonyFinalizer]
        static Exception Finalizer(SomeType __instance, Exception __exception)
        {
            if (__exception == null) return null;
            if (IsOurInstance(__instance)) return null; // Swallow NRE
            return __exception; // Pass through for others
        }
    }
    ```

### Harmony Finalizer Limitations with Il2Cpp

!!! warning "Gotcha"
    Harmony `[HarmonyFinalizer]` catches managed-side exceptions AFTER the Il2Cpp trampoline.

    Native Il2Cpp NullRefs are wrapped as `Il2CppInterop.Runtime.Il2CppException`, **NOT** `System.NullReferenceException`.
    A finalizer checking only `is NullReferenceException` will miss them entirely.

    **Pattern**: check both types in finalizers:

    ```csharp
    if (__exception is NullReferenceException)
        return null;
    if (__exception is Il2CppException &&
        __exception.Message.IndexOf("NullReferenceException", StringComparison.Ordinal) >= 0)
        return null;
    ```

    Requires `using Il2CppInterop.Runtime;`.

### Defensive Sorting of Il2Cpp Collections

When sorting or iterating collections that contain Il2Cpp proxy objects (e.g., stations, drones):

- `GetPosition()` or any property access on a destroyed Il2Cpp proxy throws native NullRef
- `.Where(s => s != null)` catches managed nulls but NOT destroyed Il2Cpp proxies
- ALWAYS wrap LINQ sort chains in try/catch when they call Il2Cpp methods
- Fallback: use the unsorted collection (degraded but functional)

```csharp
try
{
    Vector3 pos = drone.GetPosition();
    this.stations = availStations
        .Where(s => s != null)
        .OrderBy(s => Vector3.SqrMagnitude(pos - s.GetPosition()))
        .ToList();
}
catch (Exception ex)
{
    MelonLogger.Warning("Sort failed: " + ex.Message);
    this.stations = availStations; // fallback to unsorted
}
```

### LINQ Field vs Return Value

When using LINQ `orderby b.field` on bid objects:

- LINQ reads the **field**, not the method return value
- `Evaluate()` must store `this.score = ComputeScore()` — just `return ComputeScore()` leaves the field at default `0f`
- All bids sort the same → effectively random selection
- Pattern: always `this.field = ComputeValue(); return this.field;`

### Game Rendering Distance Systems Use Camera.main

ALL game distance-based rendering systems use `Camera.main` position:

- `UnluckDistanceDisabler`: `_distanceFromMainCam` (bool) + `CheckDisable()`/`CheckEnable()`
- `AvatarImpostor`: native `LateUpdate` computes distance from Camera.main for billboard/3D switch
- LOD groups / Occlusion Culling: standard Unity, Camera.main-based

!!! tip "Key Insight"
    Game Freecam works because it moves Camera.main itself. A separate drone camera does NOT influence these systems.

**Solution (CHANGE-023)**: Disable `PlayerCamera` component (stops Camera.main from following player), then reposition Camera.main to drone position each frame via `OnLateUpdate()`.

**Failed approaches:**

- `AvatarImpostor.cachedCamera` redirect: Code ran (64 redirects logged), NPCs stayed 2D. Native `LateUpdate` does NOT use `cachedCamera` for distance checks.
- `AvatarImpostor.EnableImpostor()` Harmony Prefix: No effect — native LateUpdate calls EnableImpostor internally, bypassing managed Harmony proxy.
- `AvatarImpostor.DisableImpostor()` per-frame call: Employees disappeared entirely (destructive side effect), 19x NullRef at shutdown, stutter.
- Direct `cullingMask = -1`: Only affects layer visibility, not distance-based rendering decisions.

**Working approach (CHANGE-029)**: Prefix on `LateUpdate` with `meshRenderer.enabled = false` — hides billboard quad directly, LODGroup shows 3D mesh via Camera.main positioning (CHANGE-023).

### Harmony Patch Target Selection in Il2Cpp

!!! danger "Critical"
    Native-to-native calls within Il2Cpp bypass managed Harmony patches entirely. If a patched method is called from native code internally (e.g., `EnableImpostor()` called from native `LateUpdate()`), the Harmony prefix/postfix NEVER fires.

    **Rule**: Always patch the **Unity lifecycle entry point** (`LateUpdate`, `Update`, `FixedUpdate`, `OnEnable`, etc.) — these are guaranteed to go through the managed proxy.

    **Rule**: Prefer direct field manipulation (e.g., `meshRenderer.enabled = false`) over calling native methods (e.g., `DisableImpostor()`) in per-frame patches.

    **Example (CHANGE-029)**:
    - FAILED: Prefix on `EnableImpostor` — no effect, called from native LateUpdate
    - FAILED: Prefix on `LateUpdate` with `DisableImpostor()` call — Employees vanished, 19x NullRef, stutter
    - WORKING: Prefix on `LateUpdate` with `meshRenderer.enabled = false` — direct field toggle, no side effects

### Il2Cpp Type Ambiguity: Vector3.sqrMagnitude

!!! warning "Gotcha"
    `Vector3.sqrMagnitude` is **AMBIGUOUS in Il2Cpp** (BC31429 — property + field coexist). Use `Vector3.Dot(v, v)` instead for squared distance calculations.

### GameInput Exit Listener Registration (Scene-Reload Safety)

**Context**: `GameInput.RegisterExitListener(ExitDelegate, int priority)` registers a callback for right-click/ESC exit events.

**Rules:**

| Rule | Why |
|------|-----|
| Register in `OnSceneWasLoaded("Main")`, NOT in `OnInitializeMelon` | GameInput instance may not exist before scene load |
| Deregister in cleanup / `OnSceneWasUnloaded` | Stale delegates cause NullRef when GameInput invokes them after scene reload |
| Always check `action.used` at the TOP of the handler | A higher-priority listener may have already consumed the event |
| Call `action.Use()` BEFORE performing your close logic | Prevents race conditions where phone app also closes |
| Do NOT hold Il2Cpp object references in the delegate closure | Scene reload destroys GameObjects → stale Il2Cpp pointers in closure → crash |

**Priority guidelines:**
- Default phone app handlers use standard priority (≈0)
- Mod sub-menus / overlays should use higher priority (5-10) to intercept before the app closes
- Mod UIs that should close AFTER phone apps use lower priority (negative)

### Unity Input System Same-Frame Dual Callbacks (ESC Key)

!!! warning "Gotcha"
    The ESC key is bound to TWO separate Unity Input System actions: `OnEscape` AND `OnTogglePauseMenu`. Both fire as independent callbacks in the **same frame**.

    **Root cause**: The game's own `OnEscape` sets `GameInput.TogglePauseInputUsed = true` — a static coordination flag. When our Harmony prefix returns `false` (blocking original `OnEscape`), the flag is **never set**, and `OnTogglePauseMenu` opens the pause menu.

**Solution — Set TogglePauseInputUsed (game's native flag)**:

```csharp
[HarmonyPatch("OnEscape")]
[HarmonyPrefix]
static bool OnEscape(GameInput __instance)
{
    if (!MyApp.IsActive) return true;
    GameInput.TogglePauseInputUsed = true;   // tell game's OnTogglePauseMenu to skip
    __instance.Exit(ExitType.RightClick);
    return false;
}

[HarmonyPatch("OnTogglePauseMenu")]
[HarmonyPrefix]
static bool OnTogglePauseMenu()
{
    if (MyApp.IsActive) return false;          // safety belt while app is active
    return true;                                // game's own TogglePauseInputUsed check handles it
}
```

**Rules:**

| Rule | Why |
|------|-----|
| Always patch BOTH `OnEscape` AND `OnTogglePauseMenu` | ESC fires both — patching only one leaves the other unblocked |
| Set `TogglePauseInputUsed = true` in OnEscape prefix | The game's native coordination flag |
| Set the flag BEFORE calling `Exit()` / `CloseApp()` | Ensures the flag is active when `OnTogglePauseMenu` fires |
| Keep `IsActive` check in OnTogglePauseMenu as safety belt | Belt-and-suspenders in case the flag is consumed before OnTogglePauseMenu fires |

### Il2Cpp String Marshaling in Per-Frame Loops (Performance)

!!! tip "Performance"
    Accessing `.name` on Il2Cpp `Transform`/`GameObject` triggers string marshaling (native → managed copy) EVERY call.

    In per-frame loops iterating `PoIContainer.GetChild(i).name`, this creates ~30-60 string allocations per frame.

    **Pattern**: Read `.name` ONCE on first encounter, classify by `GetInstanceID()` into cached `HashSet<int>` sets (skip, classified, player). Subsequent frames use O(1) `Contains(int)` — zero string marshaling.

### Camera.main Caching in Per-Frame Code

- `Camera.main` is expensive per-frame: iterates all cameras, finds one tagged "MainCamera"
- `Camera.allCameras` allocates a new array every call — even worse
- **Pattern**: Cache `Camera.main` result, revalidate every N frames (e.g., 60) or when cached ref becomes null/disabled

### Unity API Gotchas in Il2Cpp

#### AudioSource ignores timeScale

- `Time.timeScale = 0` does NOT pause `AudioSource` playback
- Must manually call `AudioSource.Pause()` / `AudioSource.UnPause()`
- Check `Time.timeScale == 0` in Update loop and pause/resume accordingly

#### DownloadHandlerAudioClip Constructor

- `DownloadHandlerAudioClip..ctor` throws `MissingMethodException` in Il2Cpp
- Use `UnityWebRequestMultimedia.GetAudioClip()` factory method instead
- OGG loading also broken — use WAV (direct byte-parser) or MP3 (via ffmpeg pipe)

#### URP Shader Compatibility

- Standard Shader with Fade mode (`_Mode=2`) does NOT render in URP pipeline
- Use `Sprites/Default` shader — pipeline-agnostic, works in both Built-in and URP
- Fallback chain: `Shader.Find("Sprites/Default")` then `Shader.Find("Unlit/Transparent")`

#### Child-GO for Component Offset

- `AddComponent<Light>()` on a GO shares the GO's Transform
- Setting `localPosition` on the component moves the ENTIRE GO, not just the component
- **Solution**: Create a child GameObject, set `localPosition` on the child, add the component to the child

#### DestroyImmediate vs Destroy for Colliders

- `Destroy(collider)` delays destruction by 1 frame
- During that frame, raycasts will still hit the collider (e.g., fog quads detected as 'ground')
- **Solution**: Use `DestroyImmediate(collider)` for colliders removed at spawn time

#### DestroyImmediate vs Destroy for Components with Deferred Lifecycle

- `Destroy(component)` is deferred to frame-end — the component remains alive and its Unity lifecycle continues
- If a `MonoBehaviour` is `Destroy()`-ed, its `Start()` can still fire before the actual destruction
- This allows Harmony Postfixes on `Start()` to run on components that were "destroyed" but haven't been cleaned up yet

### S1API NPCPrefabBuilder.EnsureSmokeBreak() — Cigarette NOT wired correctly

!!! warning "Gotcha"
    Custom NPC plays the smoking animation but holds **no visible cigarette**.

    **Cause**: `EnsureSmokeBreak()` sets a field named `CigarettePrefab` via reflection. This field does not exist there. Vanilla uses `_cigarette` (type `EquippableData`). Additionally, `Resources.Load("GameObject/Cigarette_Lit")` lookup fails.

    **Fix pattern**: After each scene load, start a coroutine that uses `FindObjectsOfType<SmokeCigarette>(true)` to find a vanilla donor with a populated `_cigarette` and copies the value via reflection to all custom NPC `SmokeCigarette` components where `_cigarette == null`.

### MelonMod Name Collisions with System Types

#### `Assembly` Property Ambiguity

- `MelonBase` has an own `Assembly` property (deprecated, replaced by `MelonAssembly.Assembly`)
- Using `Assembly.GetExecutingAssembly()` inside a `MelonMod` subclass resolves to the instance property, NOT `System.Reflection.Assembly`
- **Solution**: Always fully qualify as `System.Reflection.Assembly.GetExecutingAssembly()` etc.

#### `Object` Ambiguity with `using System;`

- Adding `using System;` in a MelonMod file causes `Object` to be ambiguous between `UnityEngine.Object` and `System.Object`
- **Solution**: Avoid `using System;` in MelonMod files. Use fully qualified `System.Exception`, `System.AppDomain` etc. instead.

### Embedding External DLLs in MelonLoader Mods

- External managed DLLs (e.g., NLayer for MP3 decoding) should be embedded as `EmbeddedResource` in the mod DLL
- Use `AppDomain.CurrentDomain.AssemblyResolve` handler registered in `OnInitializeMelon` to load from embedded resources
- Resource name format: `{RootNamespace}.{FolderPath}.{FileName}` (e.g., `rave.lib.NLayer.dll`)
- Cache the loaded Assembly in a static field to avoid re-loading on subsequent resolves

### MP3 Decoding in Il2Cpp

- **NLayer** (managed MPEG decoder) works reliably in MelonLoader/Il2Cpp for MP3 playback
- Replaces ffmpeg-pipe approach (no external process, no install dependency)
- `MpegFile.ReadSamples()` outputs float PCM directly — feed into `AudioClip.SetData()`
- `MpegFile.Duration` may be approximate — use dynamic buffer with resize fallback

### Shader Selection for Runtime-Created 3D Meshes (URP)

| Shader | Result | Use Case |
|--------|--------|----------|
| `Shader.Find("Universal Render Pipeline/Lit")` | Opaque, lit, correct | 3D meshes (DJ booth, props) |
| `Shader.Find("Sprites/Default")` | Ghostly/transparent, no depth write | 2D sprites, fog planes |
| `Shader.Find("Standard")` | Null or broken in URP | **DO NOT USE** |
| Primitive shader steal (CreatePrimitive) | Returns 'Standard' | **DO NOT USE** |

`Shader.Find('Universal Render Pipeline/Lit')` is the correct shader for opaque runtime-created 3D geometry. `_BaseColor` is the URP/Lit color property.

## See Also

- [Modding Guide](modding-guide.md) — General modding setup and Harmony basics
- [Safety Concept](safety-concept.md) — Comprehensive safety guidelines for Il2Cpp mods

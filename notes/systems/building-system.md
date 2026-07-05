---
title: "Building/Construction System"
description: "Complete reference for the building system — foundations, walls, tiles, navmesh, and construction lifecycle"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Building/Construction System

## BuildManager (`Il2CppScheduleOne.Building.BuildManager`)
`NetworkSingleton<BuildManager>`. Singleton managing all building/placement.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `isBuilding` | `bool` (get) | Whether player is in build mode |
| `currentBuildHandler` | `GameObject` (get) | Active build handler |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `PlaceSounds` | `List<BuildSound>` | Sound effects per build type |
| `ghostMaterial_White` | `Material` | Valid placement material |
| `ghostMaterial_Red` | `Material` | Invalid placement material |

### Methods
| Method | Description |
|--------|-------------|
| `StartBuilding(ItemInstance item)` | Enter build mode for an item |
| `StopBuilding()` | Exit build mode |
| `PlayBuildSound(EBuildSoundType, Vector3)` | Play placement sound |
| `DisableColliders(GameObject)` | Disable all colliders (for ghost) |
| `DisableLights(GameObject)` | Disable all lights (for ghost) |

---

## Build States (State Machine)
Build handler uses a 3-phase state machine:

### BuildStart_Base (`Il2CppScheduleOne.Building.BuildStart_Base`)
Phase 1: Initialize building.

| Subclass | Placement Type |
|----------|----------------|
| `BuildStart_Grid` | Grid-based placement (furniture, stations) |
| `BuildStart_Surface` | Surface-based placement (on counters, tables) |
| `BuildStart_ProceduralGrid` | Procedural grid placement |
| `BuildStart_AirConditioner` | Special AC placement |

### BuildUpdate_Base (`Il2CppScheduleOne.Building.BuildUpdate_Base`)
Phase 2: Ghost placement loop — runs per frame while building.

| Subclass | Description |
|----------|-------------|
| `BuildUpdate_Grid` | Grid snapping + ghost validation |
| `BuildUpdate_Surface` | Surface detection + placement |
| `BuildUpdate_ProceduralGrid` | Procedural grid alignment |
| `BuildUpdate_GrowContainer` | Special grow container placement |
| `BuildUpdate_AirConditioner` | AC unit placement |

### BuildStop_Base (`Il2CppScheduleOne.Building.BuildStop_Base`)
Phase 3: Finalize placement.

| Subclass | Description |
|----------|-------------|
| `BuildStop_AirConditioner` | AC placement finalize |

### Methods (all phases)
| Method | Description |
|--------|-------------|
| `StartBuilding(ItemInstance)` | Begin build |
| `Stop_Building()` | Cancel/destroy build handler |

---

## Surface (`Il2CppScheduleOne.Building.Surface`)
Defines valid placement surfaces for surface-based items.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| (serialized) | | Surface bounds, normal, supported items |

---

## TileIntersection / CornerObstacle
Grid collision detection for tile-based placement.

---

## OverrideGhostMaterial (`Il2CppScheduleOne.Building.OverrideGhostMaterial`)
Component to override ghost material color.

---

## Door Subfolder (`Il2CppScheduleOne.Building.Doors`)
Building-related door components.

| Class | Description |
|-------|-------------|
| (door-specific build handlers) | Handles door placement in build mode |

See also the main Door system docs for runtime door classes: `PivotDoor`, `SlidingDoor`, `RollerDoor`, `StaticDoor`, `SensorRollerDoors`, `DoorController`, `ManholeCoverMovement`, `Peephole`, etc.

---

## Key Information (non-Building folder)

### BuildableItemDefinition (`Il2CppScheduleOne.ItemFramework.BuildableItemDefinition`)
Item definition for placeable objects.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `BuiltItem` | `BuildableItem` | Prefab placed in world |
| `BuildHandler` | `GameObject` | Handler for ghost/placement |
| `EBuildSoundType` | enum | Sound to play on place |

### EBuildSoundType Enum
| Value | Description |
|-------|-------------|
| `Plastic` | Plastic object sound |
| `Metal` | Metal object sound |
| `Wood` | Wood object sound |
| (others) | Per-asset sounds |

### GridItem (`Il2CppScheduleOne.ObjectScripts.GridItem`) extends NetworkBehaviour
Base for all grid-placed items.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `GUID` | `string` | Unique identifier |
| `OwnerGrid` | `Grid` | Grid this item belongs to |
| `CoordinatePairs` | List | Grid coordinate pairs |
| `NetworkObject` | `NetworkObject` | FishNet network object |

### Methods
| Method | Description |
|--------|-------------|
| `SetGUID(string)` | Set GUID (does NOT register) |
| `InitializeGridItem(ItemInstance, Grid, Vector2, int, string)` | Full init + GUID registration |

### Tile System (`Il2CppScheduleOne.Tiles`)
| Component | Description |
|-----------|-------------|
| `Tile` | Grid tile, foot traffic, light exposure |
| `FootprintTile` | Marks occupied building footprint |
| `IndoorTile` | Marks indoor tile (used by drone stations) |
| `Tile.GetLightExposure()` | Light level for growing |

### Tile Cloning Cleanup
When cloning grid objects (e.g., storage racks), remove footprint tiles to prevent conflicts:
```csharp
// Must use DestroyImmediate — Destroy() is deferred and the tile remains active for 1 frame
foreach (var ft in obj.GetComponentsInChildren<FootprintTile>(true))
    DestroyImmediate(ft);
```

---

## Outlinable (EPOOutline)
`Il2CppEPOOutline.Outlinable` — used by Clipboard for selection highlighting.

When cloning buildable items:
- The clone often carries **multiple** `Outlinable` components from the template
- Keep **one** (disabled) for clipboard compatibility, destroy extras
```csharp
var outlinables = obj.GetComponentsInChildren<Outlinable>(true);
for (int i = 1; i < outlinables.Length; i++)
    DestroyImmediate(outlinables[i]);
outlinables[0].enabled = false; // enable when clipboard selects it
```

---

## ObjectField / ConfigurationReplicator
For custom buildables that participate in Clipboard routing (e.g., storage connections):

| Class | Namespace | Purpose |
|-------|-----------|---------|
| `ObjectField` | `Il2CppScheduleOne.Management` | References another `BuildableItem` by GUID |
| `ConfigurationReplicator` | `Il2CppScheduleOne.Management` | Syncs ConfigFields via FishNet RPCs |

### Critical: FishNet Serialization
FishNet serializes `NetworkBehaviour` by `NetworkObject.ObjectId`, **NOT** GUID.
- Non-networked targets (no `NetworkObject`) serialize as **null** through RPCs
- `ConfigurationReplicator.ReplicateField` → RPC roundtrip → null overwrites your value
- **Fix**: use `network: false` in `ObjectField.SetObject(obj, network: false)` for non-networked targets
- GUIDManager registration is still needed for save/load (`ObjectField.Load` resolves by GUID)

### GUID Lifecycle — Deregistration Required
`SetGUID(guid)` only sets the backing field. Registration requires explicit call:
```csharp
// Registration (done in InitializeGridItem normally)
item.SetGUID(guid);
GUIDManager.RegisterObject(item.Cast<IGUIDRegisterable>(), item.gameObject);

// Deregistration — MUST be in BOTH Cleanup() AND Destroy()
// Prevents stale GUID entries across scene reloads
GUIDManager.DeregisterObject(item.Cast<IGUIDRegisterable>());
```

---

## Save/Load Pipeline for Buildables
A placed `GridItem` survives save/reload through:

1. **Save**: `GridItem` → serializes GUID + grid coordinates + rotation + item instance into save JSON
2. **Load**: `PlaceableStorageEntityLoader.Load` (or custom loaders) → `Registry.GetItem(id)` → `Instantiate(prefab)` → `InitializeGridItem(instance, grid, coord, rotation, GUID)`
3. **Key methods**: `GridItem.InitializeGridItem()` registers with GUIDManager and restores state
4. **Custom buildables**: If you bypass `InitializeGridItem`, you must manually call `GUIDManager.RegisterObject()`

---

## Scene Reload Safety Checklist
When creating custom buildable objects, ensure:

- [ ] **Cleanup path**: Deregister from GUIDManager in both `Cleanup()` and `Destroy()`
- [ ] **Recreation path**: `Load()` → `Instantiate()` → setup methods re-apply from scratch
- [ ] **No stale static references**: Scene reload destroys all GameObjects; static refs → crash
- [ ] **FootprintTile cleanup**: `DestroyImmediate()` on cloned tiles
- [ ] **Outlinable dedup**: Keep one, destroy extras
- [ ] **NetworkObserver removal**: `DestroyImmediate()` on non-networked clones

---

## Il2Cpp Registration for Custom Build Phases
If you create custom `BuildStart_Base` / `BuildUpdate_Base` subclasses:

```csharp
[RegisterTypeInIl2Cpp]
public class MyBuildHandler : BuildUpdate_Base
{
    // REQUIRED: IntPtr constructor for Il2Cpp interop
    public MyBuildHandler(IntPtr ptr) : base(ptr) { }

    // REQUIRED for managed-side instantiation
    public MyBuildHandler() : base(ClassInjector.DerivedConstructorPointer<MyBuildHandler>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
}
```
Registration in `OnLateInitializeMelon`:
```csharp
ClassInjector.RegisterTypeInIl2Cpp<MyBuildHandler>();
```

---

## ⚠️ CRITICAL: Never Use AddComponent for GridItem/BuildableItem

Il2Cpp game components like `GridItem`, `BuildableItem`, `PlaceableStorageEntity` **cannot** be created via `AddComponent<T>()`.

| Issue | Cause |
|-------|-------|
| `AddComponent<BuildableItem>()` on inactive GO | Returns **null** (FishNet NetworkBehaviour requires active GO) |
| `AddComponent<BuildableItem>()` on active GO | `GridItem.Awake()` NREs immediately (7+ NullReferenceExceptions — expects full game init) |
| Same applies to: `FootprintTile`, `TileAppearance`, `TileDetector`, `CornerObstacle`, `StorageEntity`, `PlaceableStorageEntity` | All have complex `Awake()` methods |

**Rule**: Always clone from existing scene objects via `Instantiate<T>()` or `Instantiate(prefab)`. Never use `AddComponent<T>()` for Il2Cpp game components.

---

## Usage Patterns
```csharp
// From item in inventory
ItemInstance item = playerInventory.GetItemInSlot(slotIndex);
Singleton<BuildManager>.Instance.StartBuilding(item);

// Check if building
bool isBuilding = Singleton<BuildManager>.Instance.isBuilding;
```

### Cancelling build
```csharp
Singleton<BuildManager>.Instance.StopBuilding();
```

### Harmony patching build start/stop
```csharp
[HarmonyPatch(typeof(BuildManager), nameof(BuildManager.StartBuilding))]
public class Patch_BuildStart
{
    [HarmonyPrefix]
    static void Prefix(ItemInstance item)
    {
        Melon<MyMod>.LoggerInstance.Msg($"Building started: {item?.Name}");
        // Custom logic before building begins
    }
}

[HarmonyPatch(typeof(BuildManager), nameof(BuildManager.StopBuilding))]
public class Patch_BuildStop
{
    [HarmonyPrefix]
    static void Prefix()
    {
        // Intercept building cancellation
    }
}
```

### Placing custom networked objects
```csharp
// CRITICAL: NEVER use AddComponent<T>() for GridItem/BuildableItem
// Their Awake() methods expect full game initialization and will NRE.
// ALWAYS clone from an existing scene object via Instantiate().

// GridItem must be registered with GUIDManager
item.SetGUID(guid);
GUIDManager.RegisterObject(item.Cast<IGUIDRegisterable>(), item.gameObject);

// For non-networked objects, destroy network components
DestroyImmediate(item.GetComponent<NetworkObserver>());
DestroyImmediate(item.GetComponent<NetworkTransform>());

// Deregister on cleanup
GUIDManager.DeregisterObject(item.Cast<IGUIDRegisterable>());
```

### Checking tile properties
```csharp
Tile tile = grid.GetTile(coordinate);
float lightExposure = tile.LightExposureNode.GetTotalExposure(out float speedMult);
bool isIndoor = tile.GetComponent<IndoorTile>() != null;
```

---

## Advanced: Synthetic Grids — Placing Outside Owned Properties

!!! tip "Community Technique"
    This technique was pioneered by **SidewalkEconomy** (by Druha) and **S1MAPI** (by IfBars). It allows placing buildable items on any location on the map, not just within owned properties.

### The Problem

Vanilla `BuildUpdate_Grid.CheckIntersections()` only validates placement against **existing property grids**. If the player isn't inside an owned property, placement is always invalid — the game has no grid to snap to.

### The Solution: Synthetic Grids

Create temporary `Property` + `Grid` objects at the placement location on-the-fly:

```
1. Player enters build mode outside a property
2. On each frame, create a hidden "synthetic" Property + Grid at the ghost position
3. Add Tile GameObjects with BoxColliders at grid coordinates
4. Vanilla CheckIntersections runs against these synthetic tiles → placement valid
5. On confirm: promote the preview grid to a permanent synthetic property
```

### Implementation Architecture

**Marker Components** (empty Il2Cpp-registered MonoBehaviours used for identification):

| Component | Marks | Purpose |
|-----------|-------|---------|
| `SyntheticGridMarker` | Grid | Identifies dynamically created grids |
| `SyntheticTileMarker` | Tile | Identifies dynamically created tiles |
| `SyntheticPropertyMarker` | Property | Identifies dynamically created properties |
| `SyntheticPreviewTileMarker` | Tile | Marks preview tiles before permanent placement |

**Core Manager** — static singleton that owns all synthetic grids:

```csharp
public static class SyntheticGridManager
{
    private static GameObject _root;
    private static List<SyntheticGridInfo> Grids = new();

    public static bool EnsureGridForPlacement(Vector3 pos, Vector3 origin, GridItem buildable)
    {
        // 1. Find existing grid near position (25m radius)
        var info = FindGridNearPosition(pos, 25f);

        // 2. If none exists, create a synthetic property + grid at origin
        if (info == null)
        {
            info = CreateSyntheticGrid(origin);
            Grids.Add(info);
        }

        // 3. Extend grid bounds to cover the buildable's footprint
        ExtendGridBounds(info, GetPlacementGridBounds(buildable, pos));
        return true;
    }
}
```

**Synthetic Property Creation**:

```csharp
private static Property CreateSyntheticProperty(Vector3 origin, string code, string name)
{
    var go = new GameObject(name);
    go.transform.SetParent(_root.transform, false);
    go.transform.position = origin;

    // Mark as synthetic
    AddInjectedComponent<SyntheticPropertyMarker>(go);

    // Add vanilla Property component (works without scene Property)
    var property = go.AddComponent<Property>();
    SetPropertyField(property, "propertyCode", code);
    SetPropertyField(property, "propertyName", name);
    SetPropertyField(property, "IsOwned", true);
    SetPropertyField(property, "AmbientTemperature", 20f);
    property.EmployeeCapacity = 0;

    // Register with vanilla lists so employees/routing see it
    Property.Properties.Add(property);
    return property;
}
```

**Synthetic Grid + Tile Creation**:

```csharp
private static Grid CreateSyntheticGrid(Property property, Vector3 origin)
{
    var go = new GameObject(property.PropertyName + "_Grid");
    go.transform.SetParent(property.Container.transform, false);
    go.transform.position = origin;

    AddInjectedComponent<SyntheticGridMarker>(go);
    var grid = go.AddComponent<Grid>();

    property.Grids.Add(grid);
    return grid;
}

private static Tile CreateTile(Grid grid, int x, int y)
{
    var go = new GameObject($"SyntheticTile_{x}_{y}");
    go.transform.SetParent(grid.transform, false);
    go.transform.position = GetTerrainAlignedTilePosition(grid, x, y);
    go.layer = LayerMask.NameToLayer("Tile");

    AddInjectedComponent<SyntheticTileMarker>(go);

    var bc = go.AddComponent<BoxCollider>();
    bc.size = new Vector3(0.5f, 0.1f, 0.5f);
    bc.isTrigger = true;

    var tile = go.AddComponent<Tile>();
    tile.InitializePropertyTile(x, y, 1000f, grid);

    var lightNode = go.AddComponent<LightExposureNode>();
    lightNode.ambientExposure = 1f;
    tile.LightExposureNode = lightNode;

    return tile;
}
```

### Harmony Patches Required

**1. `BuildUpdate_Grid.CheckIntersections` (Postfix)** — Validate against world colliders, not just property tiles:

```csharp
[HarmonyPatch(typeof(BuildUpdate_Grid), nameof(BuildUpdate_Grid.CheckIntersections))]
static class BuildUpdateGridCheckPatch
{
    static void Postfix(BuildUpdate_Grid __instance)
    {
        if (!ModSettings.EnableOutsidePlacement) return;
        if (!IsPlacementValid(__instance)) return;

        var buildable = __instance.BuildableItemClass;
        if (!SyntheticGridManager.HasAnyTileIntersections(buildable))
            SetPlacementValid(__instance, false);
        else if (HasBlockingWorldIntersection(buildable))
            SetPlacementValid(__instance, false);
    }
}
```

**2. `BuildUpdate_Grid.Update` (Prefix + Postfix)** — Drive preview grid + placement confirmation:

```csharp
[HarmonyPatch(typeof(BuildUpdate_Grid), "Update")]
static class BuildUpdateGridPlacePatch
{
    static void Postfix(BuildUpdate_Grid __instance)
    {
        // Every frame: ensure preview grid exists at ghost position
        var buildable = __instance.BuildableItemClass;
        if (buildable != null && !IsPlacementValid(__instance))
        {
            SyntheticGridManager.EnsurePreviewGridForPlacement(
                placementPosition, originPosition, buildable);
            BuildModeRecovery.ReportOutsidePlacementHeartbeat();
        }

        // On left-click confirm: promote preview to permanent
        if (GameInput.GetButtonDown(0) && IsPlacementValid(__instance))
        {
            SyntheticGridManager.PromotePreviewGridForPlacement(
                placementPosition, originPosition, buildable);
            Physics.SyncTransforms();
            buildable.CalculateFootprintTileIntersections();
        }
    }
}
```

### Save/Load

Synthetic grids must be serialized and restored across game sessions:

```csharp
[Serializable]
public class SyntheticGridRecord
{
    public string PropertyCode, PropertyName, GridGuid;
    public Vector3 Origin;
    public int MinX, MaxX, MinY, MaxY;
    public bool HasManualIdlePoint;
    public Vector3 ManualIdlePointPosition;
    public float ManualIdlePointYaw;
}

// Save: capture all synthetic grids
SyntheticGridManager.CaptureForSave();

// Load: restore all synthetic grids from records
SyntheticGridManager.RestoreFromSave(savedRecords);
```

### Key Design Considerations

| Challenge | Solution |
|-----------|----------|
| Placement outside property = always invalid | Create synthetic Property at placement position |
| No grid to snap to | Create synthetic Grid + Tiles on-the-fly |
| Ghost validation needs world collision check | OverlapBox/Sphere against world colliders |
| Scene reload destroys everything | `DontDestroyOnLoad(_root)`, save/load via JSON |
| Employees should not route to synthetic properties | `EmployeeCapacity = 0`, `SyntheticPropertyMarker` check |
| Preview vs permanent distinction | `SyntheticPreviewTileMarker` on preview tiles, promoted on confirm |

### Limitations

- **Employees**: Cannot be assigned to synthetic properties without additional patching (destination system expects `NetworkBehaviour`)
- **NavMesh**: Synthetic grids don't create NavMesh — NPCs may not pathfind to them
- **Performance**: Each tile is a separate GameObject with collider — large builds may impact performance
- **Multiplayer**: Synthetic grids are local-only unless network objects are also created

---

## Advanced: Custom Interiors — Remodeling Existing Buildings

!!! tip "Community Technique"
    Pioneered by **The Mob Ndrangheta** mod (by Druha). This technique repurposes existing game buildings by destroying their vanilla interior walls/furniture and replacing them with custom rooms, grids, and navigation meshes.

### The Concept

Instead of creating new buildings from scratch (which requires 3D models, textures, and AssetBundles), you can **take over an existing building** in the game world and rebuild its interior:

```
1. Locate the target building (e.g., a Villa with NPCEnterableBuilding)
2. Purchase the property via RealEstateManager or vanilla
3. Destroy vanilla interior walls + furniture
4. Create new rooms with custom walls, floors, ceilings
5. Add Grid objects with Tiles inside the rooms for buildable placement
6. Add NavMesh obstacles/carvers for NPC pathfinding
7. Register doors for access control
```

### Core Architecture

**Interior Root** — All custom objects are parented under a single root GameObject:

```csharp
// Create interior root at the building's position
_root = new GameObject("Ndrangheta_VillaInterior");
_root.transform.position = interiorWorldPoint;

// Create room-specific grids (children of the root)
BuildSalonGrid(ownedProperty);
BuildTerraceGrid(ownedProperty);
```

**Vanilla Wall Removal + Custom Walls** — Destroy existing interior geometry, replace with sealed walls:

```csharp
// Track original materials for restoration on scene reload
private static Dictionary<Renderer, Material> _origMat = new();

// Destroy vanilla furniture
foreach (var furniture in _villaFurniture)
    Object.Destroy(furniture);

// Remove specific vanilla wall GameObjects
foreach (var wall in _vanillaWalls)
    Object.Destroy(wall);

// Create new wall "seals" to close openings
var seal = GameObject.CreatePrimitive(PrimitiveType.Cube);
seal.transform.SetParent(_root.transform, true);
// Position to fill door/window openings
```

**Room-Specific Grids** — Create Grid + Tiles inside each room:

```csharp
private static Grid BuildSalonGrid(Property ownedProp)
{
    var gridGo = new GameObject("Ndrangheta_SalonGrid");
    gridGo.transform.SetParent(_root.transform, true);
    gridGo.transform.position = roomCenter;
    gridGo.transform.rotation = Quaternion.identity;

    _salonGrid = gridGo.AddComponent<Grid>();
    GameCompat.SetGridParentProperty(_salonGrid, ownedProp);
    GameCompat.SetGridGuidString(_salonGrid, DeterministricGuid().ToString());

    // Create tiles in a grid pattern
    for (int x = 0; x < tileCountX; x++)
    for (int z = 0; z < tileCountZ; z++)
        BuildSalonTile(x, z, tileLayer);

    return _salonGrid;
}

private static bool BuildSalonTile(int gx, int gz, int layer)
{
    var go = new GameObject($"SalonTile_{gx}_{gz}");
    go.layer = layer;
    go.transform.SetParent(_salonGrid.transform, false);
    go.transform.localPosition = new Vector3(gx * 0.5f, 0f, gz * 0.5f);

    var bc = go.AddComponent<BoxCollider>();
    bc.size = new Vector3(0.5f, 0.1f, 0.5f);
    bc.isTrigger = true; // trigger = tile detection, non-trigger = physical floor

    var tile = go.AddComponent<Tile>();
    tile.InitializePropertyTile(gx, gz, 1000f, _salonGrid);
    _salonGrid.RegisterTile(tile);
    return true;
}
```

**Grid Persistence** — Serialize buildable items placed on custom grids:

```csharp
public static string CaptureAllString()
{
    var sb = new StringBuilder();
    CaptureGrid(LagerGrid.GridRef, GridKey.Lager, sb, processedGuids);
    CaptureGrid(VillaRoom.TerraceGridRef, GridKey.Terrace, sb, processedGuids);
    CaptureGrid(VillaRoom.SalonGridRef, GridKey.Salon, sb, processedGuids);
    // ... also captures items not yet in a live grid (stash)
    return sb.ToString();
}

// Format: KEY|originX|originZ|rotation|guid|itemData_base64|contents_base64
// Example: 2|123.45|678.90|0|guid123|base64item|base64contents
```

### Comparison: Synthetic Grids vs Custom Interiors

| Aspect | Synthetic Grid (SidewalkEconomy) | Custom Interior (Ndrangheta) |
|--------|----------------------------------|------------------------------|
| **Location** | Anywhere on map | Inside existing game buildings |
| **Property type** | Creates new synthetic Property | Uses existing (purchased) Property |
| **Visuals** | No visuals (tiles only) | Destroys/rebuilds walls, floors, furniture |
| **Grid parent** | `DontDestroyOnLoad` root | Room root inside building |
| **Use case** | Place items outside owned properties | Create custom rooms/buildings with interior |
| **Complexity** | Medium | High (needs 3D wall meshes, material tracking) |
| **Scene reload** | Destroy + rebuild from save | Destroy + rebuild from save, restore original materials |
| **NavMesh** | No support | Can add NavMeshObstacle/Carver |

### Key Design Considerations for Custom Interiors

| Challenge | Solution |
|-----------|----------|
| Vanilla walls block new room layout | Destroy specific wall GameObjects, track by reference |
| Vanilla furniture in the way | Destroy all furniture in the room on build |
| Holes in walls after demolition | Create "seal" GameObjects (PrimitiveType.Cube) to fill openings |
| Original materials lost after scene reload | Snapshot `Renderer.material` before changing, restore on teardown |
| Building re-appears on scene reload | Full teardown + rebuild cycle |
| Plant/GrowLight detection needs light | Add `LightExposureNode` to tiles with `ambientExposure` |
| NPCs can't walk through walls | Add `NavMeshObstacle` component to wall GameObjects |
| Players should not see unfinished interior | Disable interior root until fully built |
| Property must be owned by player | Check `Property.Properties` for ownership before building |

---

## Advanced: GLTF/GLB Model Import — Custom 3D Buildings Without Asset Bundles

!!! tip "Community Technique"
    Pioneered by **The Big Pimpin'** mod (by IfBars). This technique loads custom 3D models (GLTF/GLB) at runtime to create buildings, furniture, and decorative objects — without requiring Unity AssetBundles.

### The Concept

Instead of bundling models as Unity AssetBundles (which require the Unity Editor and build pipeline), you can ship raw GLTF/GLB files alongside your mod DLL and load them at runtime:

```
1. Export 3D model as GLTF or GLB (from Blender, Maya, etc.)
2. Embed the binary data as an EmbeddedResource in your DLL
   OR ship as standalone files next to the DLL
3. At runtime, parse the GLTF/GLB with a managed C# parser
4. Convert to Unity Mesh + Material + GameObject hierarchy
5. Apply URP-compatible shaders (e.g., Universal Render Pipeline/Lit)
6. Place in the game world at the desired position
```

### GLTF/GLB Importer Architecture

A complete managed GLTF/GLB parser operates in these phases:

**Phase 1: Parse** — Read the binary GLB (header → JSON chunk → binary chunk) or text GLTF:

```csharp
public GameObject Load(byte[] data)
{
    uint magic = BitConverter.ToUInt32(data, 0);
    if (magic == 0x46546C67) // "glTF"
        return LoadGlb(data);
    else
        return LoadGltfJson(Encoding.UTF8.GetString(data), null);
}

public GameObject LoadGlb(byte[] glbBytes)
{
    // 1. Parse 12-byte header (magic + version + length)
    // 2. Read JSON chunk → deserialize to GltfRoot
    // 3. Read binary chunk → buffer resolver
    // 4. Call ImportModel(gltfRoot, bufferResolver)
}
```

**Phase 2: Process** — Convert GLTF data structures into Unity assets:

```csharp
private GameObject ImportModel(GltfRoot root, GltfBufferResolver resolver)
{
    var rootGo = new GameObject(_options.RootName ?? "GltfModel");

    // 1. Process materials (PBR → Unity Material)
    var materials = _materialProcessor.Process(root, resolver, _options);

    // 2. Process meshes (accessors → Unity Mesh)
    var meshes = _meshProcessor.Process(root, resolver, _options);

    // 3. Process nodes (hierarchy → GameObject tree)
    var nodes = _nodeProcessor.Process(root, resolver, meshes, materials, _options);
    foreach (var node in nodes)
        node.transform.SetParent(rootGo.transform, false);

    // 4. Process animations (optional)
    if (_options.ImportAnimations)
        _animationProcessor.Process(root, resolver, nodes);

    return rootGo;
}
```

**Phase 3: Material Conversion** — Map GLTF PBR materials to URP:

```csharp
private Material CreateMaterial(GltfMaterial gltfMat)
{
    var shader = _options.Shader ?? Shader.Find("Universal Render Pipeline/Lit");
    var mat = new Material(shader);

    if (gltfMat.PbrMetallicRoughness != null)
    {
        var pbr = gltfMat.PbrMetallicRoughness;
        mat.color = new Color(pbr.BaseColorFactor[0], pbr.BaseColorFactor[1],
                              pbr.BaseColorFactor[2], pbr.BaseColorFactor[3]);
        mat.SetFloat("_Metallic", pbr.MetallicFactor);
        mat.SetFloat("_Smoothness", 1f - pbr.RoughnessFactor);

        // Load base color texture
        if (pbr.BaseColorTexture != null)
            mat.mainTexture = LoadTexture(pbr.BaseColorTexture.Index, resolver);
    }
    return mat;
}
```

### Embedding Models in Mod DLL

Ship models as embedded resources (no external files needed):

```csharp
// In .csproj:
// <EmbeddedResource Include="Models\brothel.glb" />

// Loading at runtime:
var assembly = Assembly.GetExecutingAssembly();
var resourceName = "bigpimpin.Models.brothel.glb";

using (var stream = assembly.GetManifestResourceStream(resourceName))
{
    byte[] data = new byte[stream.Length];
    stream.Read(data, 0, data.Length);

    var importer = new GltfImporter()
        .SetName("BrothelBuilding")
        .SetScale(1f)
        .SetShader(Shader.Find("Universal Render Pipeline/Lit"));

    var building = importer.Load(data);
    building.transform.position = worldPosition;
    building.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
}
```

### Comparison: All Building Techniques

| Aspect | Synthetic Grid (SidewalkEconomy) | Custom Interior (Ndrangheta) | GLTF Import (BigPimpin) |
|--------|----------------------------------|------------------------------|-------------------------|
| **Source of visuals** | None (only tiles) | Vanilla building assets | Custom 3D models (GLTF/GLB) |
| **Location** | Anywhere on map | Inside existing buildings | Anywhere |
| **Asset pipeline** | None | None (reuses game assets) | Blender → GLTF export → embed |
| **Property required?** | Creates synthetic Property | Needs purchased Property | Not necessarily |
| **Complexity** | Medium | High | Very high (full GLTF parser needed) |
| **File size impact** | Minimal | Minimal | Large (models embedded in DLL) |
| **Scene reload** | Save/Load | Save/Load + material restore | Destroy + recreate from bytes |
| **Multiplayer** | Limited | Limited | Depends on implementation |
| **Flexibility** | Low (only placement) | Medium (vanilla assets only) | High (any 3D model possible) |
| **NavMesh support** | No | Via NavMeshObstacle | Manual if needed |

### Key Design Considerations for GLTF Import

| Challenge | Solution |
|-----------|----------|
| GLTF binary format parsing | Parse 12-byte header, JSON chunk (UTF-8), binary chunk |
| PBR materials not compatible with URP | Convert metallic/roughness to URP Lit shader properties |
| Textures not loaded automatically | Manual texture extraction from GLTF buffer views → `Texture2D.LoadRawTextureData()` |
| Large models cause frame spikes | Load in background thread or coroutine |
| Animation from GLTF | Convert to Unity AnimationClip via keyframe extraction |
| Skinned meshes | Process `JOINTS_0`/`WEIGHTS_0` accessors, create `SkinnedMeshRenderer` |
| Model scale mismatch | Configurable `ScaleFactor` option |
| Memory cleanup on scene reload | `Destroy()` all loaded GameObjects, clear material caches |

---

## Advanced: Tile Injection via Harmony — Expanding Existing Property Grids

!!! tip "Community Technique"
    Pioneered by **PropertyRenovations** mod (by Druha). This technique intercepts `Grid.Awake()` via Harmony and injects additional tiles into existing property grids — creating more buildable space without modifying the actual building mesh.

### The Concept

Vanilla properties have predefined grids with fixed tile counts. Some buildings have "dead space" (countertops, walls, support beams) that block tile placement. Instead of modifying the building model, you can:

```
1. Patch Grid.Awake() via HarmonyPostfix
2. When a target grid is detected, calculate missing tile positions
3. Clone an existing tile from the grid
4. Register the new tiles with Grid.RegisterTile()
5. Clear and rebuild the grid's internal coordinate cache
```

### Architecture

**IPropertyDefinition interface** — each property is defined by its own class:

```csharp
public interface IPropertyDefinition
{
    string Name { get; }
    bool IsTarget(Grid g);           // Does this grid belong to this property?
    Vector2Int[] Footprint { get; }  // Tile coordinates to inject
    Coordinate TemplateCoordinate { get; }  // Existing tile to clone
    Action<Grid> AfterPatch { get; }  // Optional post-injection callback
}
```

**Harmony Patch** — intercepts EVERY Grid.Awake():

```csharp
[HarmonyPatch(typeof(Grid), "Awake")]
[HarmonyPostfix]
static void Post_Awake(Grid __instance)
{
    foreach (var def in _definitions)
    {
        if (def.IsTarget(__instance))
        {
            try
            {
                InjectTiles(__instance, def);
                def.AfterPatch?.Invoke(__instance);
                break;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"{def.Name}: exception in Post_Awake\n{ex}");
                break;
            }
        }
    }
}
```

**Concrete example — Bungalow** (adds 22 tiles in the kitchen area):

```csharp
internal sealed class Bungalow : PropertyDefinitionBase
{
    public override string Name => "Bungalow";

    public override bool IsTarget(Grid g)
    {
        // Match by grid name + parent hierarchy
        if (!g.name.StartsWith("Grid (1)")) return false;
        Transform t = g.transform;
        while (t != null)
        {
            if (string.Equals(t.name, "Bungalow", StringComparison.OrdinalIgnoreCase))
                return true;
            t = t.parent;
        }
        return false;
    }

    private static readonly Vector2Int[] _footprint = new Vector2Int[]
    {
        new(0,0), new(1,0), new(2,0), new(3,0), new(4,0),
        new(5,0), new(6,0), new(7,0), new(0,1), new(1,1),
        new(2,1), new(3,1), new(4,1), new(5,1), new(6,1),
        new(7,1), new(0,2), new(1,2), new(0,3), new(1,3),
        new(0,4), new(1,4)
    };

    public override Action<Grid> AfterPatch => _ =>
    {
        // Optionally hide obstructing kitchen cabinets
        GameObject.Find("@Properties/Bungalow/bungalow/Cabinets")?.SetActive(false);
        GameObject.Find("@Properties/Bungalow/bungalow/Bench")?.SetActive(false);
    };
}
```

**Tile Injection** — clones an existing tile for each coordinate:

```csharp
private static bool InjectTiles(Grid grid, IPropertyDefinition def)
{
    if (def.Footprint == null || def.Footprint.Length == 0)
        return false;

    // Get the template tile to clone
    Tile template = grid.GetTile(def.TemplateCoordinate);
    if (template == null) return false;

    bool injected = false;
    foreach (var coord in def.Footprint)
    {
        if (grid.GetTile(new Coordinate(coord.x, coord.y)) != null)
            continue; // Tile already exists

        // Clone template tile
        var newTileGO = Object.Instantiate(template.gameObject, grid.transform);
        newTileGO.name = $"Grid [{coord.x},{coord.y}] (Injected)";
        newTileGO.transform.localPosition = new Vector3(
            coord.x * Grid.TileSize, 0f, coord.y * Grid.TileSize);

        var tile = newTileGO.GetComponent<Tile>();
        tile.InitializePropertyTile(coord.x, coord.y, template.AvailableOffset, grid);
        grid.RegisterTile(tile);
        injected = true;
    }

    if (injected)
    {
        // Clear cache so grid rebuilds coordinate mapping
        typeof(Grid).GetProperty("_coordinateToTile", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(grid)?.GetType().GetMethod("Clear")?.Invoke(null);
        grid.ProcessCoordinateDataPairs();
    }
    return injected;
}
```

**FillGridToRectangle** — expands a grid to a full rectangle, optionally extending to include a world-space position:

```csharp
protected static void FillGridToRectangle(Grid grid, Vector3? propWorldPos = null)
{
    // Find min/max X/Z of existing tiles
    // Optionally extend bounds to include propWorldPos
    // Clone missing tiles from template (0,5)
    // Clear cache + ProcessCoordinateDataPairs()
}
```

### Use Cases

| Property | Technique | Result |
|----------|-----------|--------|
| Bungalow | Inject 22 tiles in kitchen area + hide cabinets | Full use of interior space |
| Barn Upper Grid | Fill holes in upper floor | Walkable upper level with buildable space |
| Barn | `FillGridToRectangle()` | Expanded grid for more equipment |
| Sweatshop | Custom footprint injection | Additional workstations |
| Docks | Custom footprint injection | More docking/container space |
| Manor | Custom footprint injection | Extended living area |
| Storage Unit | Custom footprint injection | More storage racks |
| Sewer Office | Custom footprint injection | Extra lab space |

### Key Design Considerations

| Challenge | Solution |
|-----------|----------|
| Grid identity | Check `g.name` + walk parent hierarchy for property name |
| Grid internal cache after tile injection | `Clear()` on `_coordinateToTile` dictionary + `ProcessCoordinateDataPairs()` |
| Tile cloning | Use `Object.Instantiate(template.gameObject)` — preserves all components |
| Obstructing furniture | `GameObject.Find(path)?.SetActive(false)` in `AfterPatch` |
| Harmony execution order | Use `[HarmonyPostfix]` — runs after vanilla `Awake()` |
| Scene reload | Grid.Awake() fires again naturally — tiles are re-injected automatically |

---

## Advanced: World Editing — Finding and Modifying Scene Objects

!!! tip "Community Technique"
    Used by **OverpassMod** (collider addition) and **OverpassRemover/NoKitchen/MoreTrees** (object removal/scaling). These mods directly manipulate the static game world by finding objects in the Unity scene hierarchy and modifying them.

### Finding Scene Objects

Game world objects are nested in the scene hierarchy under `Map/Container/...`. Find them at runtime:

```csharp
// Method 1: Full path via SceneManager (works for inactive objects)
private static GameObject FindByFullPath(string fullPath)
{
    for (int i = 0; i < SceneManager.sceneCount; i++)
    {
        var scene = SceneManager.GetSceneAt(i);
        if (!scene.isLoaded) continue;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != fullPath.Split('/')[0]) continue;
            var t = root.transform;
            for (int depth = 1; depth < fullPath.Split('/').Length && t != null; depth++)
                t = t.Find(fullPath.Split('/')[depth]);
            if (t != null) return t.gameObject;
        }
    }
    return null;
}

// Method 2: Direct transform.Find() (if parent is known)
var overpass = GameObject.Find("Map/Container/Overpass/Overpass Ramp");
// This can miss inactive GameObjects — use SceneManager method for reliability
```

### Removing Objects

Hide or destroy existing world objects:

```csharp
// Hide kitchen cabinets in Bungalow (NoKitchen/PropertyRenovations)
GameObject cabinets = GameObject.Find("@Properties/Bungalow/bungalow/Cabinets");
cabinets?.SetActive(false);

// Remove bridge/overpass sections (OverpassRemover)
string[] targets = {
    "Map/Container/Overpass/Overpass Ramp/RoadBlocker4_LOD (1)",
    "Map/Container/Overpass/Overpass Ramp/RoadBlocker4_LOD (2)",
    "Map/Container/Overpass/Overpass Ramp/RoadBlocker4_LOD (3)",
    "Map/Container/Overpass/Overpass Ramp/RoadBlocker4_LOD (4)"
};
foreach (var path in targets)
{
    var go = FindByFullPath(path);
    if (go != null) Object.Destroy(go);
}
```

### Adding Colliders to World Objects

Create invisible colliders for existing meshes that lack them:

```csharp
// Spawn an invisible cube collider
private static GameObject SpawnColliderCube(GameObject parent, string name,
    Vector3 localPos, Vector3 localEuler, Vector3 localScale)
{
    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    cube.name = name;
    cube.transform.SetParent(parent.transform, false);
    cube.transform.localPosition = localPos;
    cube.transform.localEulerAngles = localEuler;
    cube.transform.localScale = localScale;

    // Hide the visual mesh
    cube.GetComponent<Renderer>().enabled = false;

    // Configure collider
    var collider = cube.GetComponent<BoxCollider>();
    collider.isTrigger = false;
    collider.enabled = true;
    return cube;
}
```

### Trigger Zones with Custom Components

Create trigger volumes that react to player proximity:

```csharp
// Create a trigger child that detects player entry
private static void AddTriggerChild(GameObject solidCube)
{
    var trigger = new GameObject(solidCube.name + "_Trigger");
    trigger.transform.SetParent(solidCube.transform, false);
    trigger.transform.localScale = new Vector3(1.05f, 1.5f, 1.05f);

    var bc = trigger.AddComponent<BoxCollider>();
    bc.isTrigger = true;

    var rb = trigger.AddComponent<Rigidbody>();
    rb.isKinematic = true;
    rb.useGravity = false;

    // Your custom Il2Cpp component
    trigger.AddComponent<TerrainToggleOnTrigger>();
}
```

### Common World-Editing Tasks

| Task | Method | Example |
|------|--------|---------|
| Find object by path | `SceneManager.GetSceneAt(i).GetRootGameObjects()` + `transform.Find()` | Overpass bridge parts |
| Hide object | `gameObject.SetActive(false)` | Bungalow kitchen cabinets |
| Destroy object | `Object.Destroy(gameObject)` | Road blockers, tree meshes |
| Add collider to mesh | `GameObject.CreatePrimitive(Cube)` + disable renderer | Overpass ramp collision |
| Add trigger zone | Create child GO with `BoxCollider` + `isTrigger=true` | Terrain toggle on overpass |
| Modify mesh scale | `transform.localScale = newScale` | Bigger trees |
| Create component at runtime | `gameObject.AddComponent<T>()` | Custom Il2Cpp MonoBehaviours |

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

Building-API in Entwicklung. Siehe [Building API Guide](../guide/building-api.md).

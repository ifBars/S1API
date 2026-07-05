---
title: "Game Source Reference"
description: "Complete class-by-class breakdown of Assembly-CSharp — critical APIs, field details, and integration notes for modding"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Game Source Reference

## Unity / GLB Runtime APIs (Project-Critical Reference)

### Mesh (`UnityEngine`)
- Properties: `vertices`, `normals`, `uv`, `tangents`, `boneWeights`, `bindposes`
- Methods: `GetBonesPerVertex()`, `GetAllBoneWeights()`, `GetTriangles(int)`
- **Key insight**: modern Unity FBX imports may provide usable skin weights only through `GetBonesPerVertex()` + `GetAllBoneWeights()`, while classic `boneWeights` can be empty or insufficient for export.
- **Key insight**: valid GLB skin export for DJs required both inverse bind matrices from `bindposes` and vertex skin attributes derived from Unity mesh weights.

### BoneWeight1 (`UnityEngine`)
- Properties: `boneIndex`, `weight`
- Returned by `Mesh.GetAllBoneWeights()`
- **Key insight**: converting this stream into classic 4-weight `BoneWeight` data was necessary so exported GLBs included `JOINTS_0` and `WEIGHTS_0`.

### SkinnedMeshRenderer (`UnityEngine`)
- Properties: `sharedMesh`, `bones`, `rootBone`, `localBounds`, `updateWhenOffscreen`
- **Key insight**: `bones` + `rootBone` are the authoritative Unity-side skin binding references for GLB `skins[].joints` and runtime `SkinnedMeshRenderer` reconstruction.
- **Gotcha**: with `Strip Bones` enabled or hierarchy not preserved, Unity can still preview animation while downstream GLB export fails to emit usable skin data.

### Animation / AnimationClip (`UnityEngine`)
- `Animation.AddClip(AnimationClip, string)`
- `Animation.Play(string)`
- `Animation.GetClipCount()`
- `Animation.clip`
- `Animation.wrapMode`
- `AnimationClip.legacy`
- `AnimationClip.wrapMode`
- **Key insight**: `Animation.Play(...)` returning true does not prove mesh deformation works; if the GLB has animated nodes but no `skins` / `JOINTS_0` / `WEIGHTS_0`, the visible character remains rigid.

### Confirmed Illegal Rave DJ GLB Workflow
- In Unity FBX import settings for `djdan` and `djgirl`: disable `Strip Bones`, enable `Preserve Hierarchy`.
- In `UnityEditorTools/GlbExporterBatch.cs`: export skins only after full hierarchy collection, support modern mesh weight APIs, and resolve joints by path/name fallback.
- Expected exported GLB validation state: `animations=1`, `skins=1`, mesh node `geometry_0` has `skin=0`, primitive attributes include `JOINTS_0` and `WEIGHTS_0`.
- Final runtime placement fix: DJs still hovered slightly after animation worked, so `RaveDJBoothController` applies `DJ_ALIGN_Y_OFFSET = -1.0f` after ground alignment.

## Economy / Dealer

### Supplier / Salvador (`Il2CppScheduleOne.Economy.Supplier`, `...NPCs.CharacterClasses.Salvador`)
- `Salvador` inherits from `Supplier` and has no custom unlock fields beyond the base supplier behavior.
- `Supplier` exposes `SendUnlocked()`, private/public wrapper `SetUnlocked()`, `SupplierUnlocked(NPCRelationData.EUnlockType type, bool notify)`, `EnableDeliveries(NetworkConnection)`, and backing fields including `_Status_k__BackingField` and `_DeliveriesEnabled_k__BackingField`. `ESupplierStatus` only has `Idle`, `PreppingDeadDrop`, and `Meeting` — no `Locked` enum value.
- NPC relationship unlock state is available via `NPC.RelationData?.IsKnown()` / `NPC.RelationData?.IsMutuallyKnown()` and `NPCRelationData.Unlock(...)`.
- **censoredMod gate note**: To gate content behind Salvador being available, check for a `Salvador` supplier instance and prefer `RelationData.IsKnown()/IsMutuallyKnown()`; if that is unavailable, a conservative runtime fallback can inspect Supplier status/deliveries fields.

### UncleNelson / CallerID (`Il2CppScheduleOne.NPCs.CharacterClasses.UncleNelson`, `Il2CppScheduleOne.ScriptableObjects.CallerID`)
- `UncleNelson` inherits from native `NPC` and is the vanilla uncle caller/contact class.
- `CallerID` has `Name` and `ProfilePicture` fields; phone call UI reads `CallerID.ProfilePicture` for call notification/interface portraits.
- Native `NPC` exposes public field-backed properties `ID`, `MugshotSprite`, `FirstName`, `LastName`, and `RelationData`.
- **censoredMod note**: Payphone CallerID should use Uncle Nelson's vanilla `MugshotSprite` when available (`FindObjectOfType<UncleNelson>()` or `NPCManager.GetNPC("uncle_nelson")`) and must not use `bigpimpin.assets.icon.png` for this vanilla character.

### Dealer (`Il2CppScheduleOne.Economy`)
- Inherits from `Il2CppScheduleOne.NPCs.NPC`; it is not a component that can simply be added to a Civilian prefab.
- Important fields/properties: `IsRecruited`, `ItemSlots`, `AssignedCustomers`, `ActiveContracts`, `DealerType`, `HomeName`, `SigningFee`, `Cut`, `CompletedDealsVariable`, `overflowSlots`, `_attendDealBehaviour`.
- Important methods: `ShouldAcceptContract(ContractInfo, Customer)`, `ContractedOffered(ContractInfo, Customer)`, `AddCustomer_Server(string)`, `RemoveCustomer(Customer)`, `CompletedDeal()`, `SubmitPayment(float)`, `GetOrderableProducts()`, `GetOrderableProductQuantity(string, EQuality, EQuality)`, `GetAvailableProducts()`, `RemoveContractItems(...)`, `AddItemToInventory(ItemInstance)`, `GetAllSlots()`.
- **S1API integration note**: custom dealer NPCs must use the Dealer network prefab path (`NPC.IsDealer == true` / `NPCPrefabBuilder.EnsureDealer()`); S1API then calls `NPCDealer.EnsureDealer()` on spawn.
- **Gotcha for service products**: native dealer orderability is inventory-slot based (`GetOrderableProducts` / `GetAvailableProducts`). Non-physical service products such as bigpimpin Date products should not be pushed into dealer inventory slots; keep the actual Date sale flow in the existing virtual Contract/Handover path.

### Equippable_RangedWeapon (`Il2CppScheduleOne.Equipping`)
- Inherits from `Equippable_AvatarViewmodel`.
- Important fields/properties: `Aim`, `AimDuration`, `MinAimFOVReduction`, `MaxAimFOVReduction`, `timeSinceAimStart`, `timeSincePrimaryClick`, `shotQueued`, `reloadQueued`, `FireCooldown`, `Ammo`.
- Important methods: `Update()`, `UpdateInput()`, `UpdateAnim()`, `CanAim()`, `Fire()`, `CanFire(bool checkAmmo = true)`, `CanCock()`, `Cock()`, `GetSpreadAngle()`.
- **Key insight for Scope mod**: `GameInput.ButtonCode.Back` / RMB belongs to the global exit/back flow and must not be treated as ranged-weapon aim. Runtime logs showed RMB made the Scope overlay visible while `Equippable_RangedWeapon.Aim` stayed `0.00`, preventing normal fire behavior.
- **Correct Scope gate**: use the weapon's real `Aim` property (`PlayerInventory.Instance.equippable.TryCast<Equippable_RangedWeapon>()?.Aim`) as source of truth for overlay/viewmodel hiding, not raw RMB or `GameInput.SecondaryClick` alone.
- **Gotcha**: Showing/hiding the scope before `Aim` rises can visually hide the weapon while Vanilla still considers the weapon not aimed; this causes confusing behavior such as left-click hold acting like aim/windup and right-click showing UI but not firing.
- **2026-05 Scope runtime note**: Pure `Aim` was still too broad because left-click/cock/recoil paths can move internal Aim-like state. Current Scope gate uses **both** real weapon `Aim` and raw `Input.GetMouseButton(1)` as a read-only RMB gate. This does not manipulate input; it only prevents LMB/cock/fire paths from triggering Scope hide/zoom.
- **FOV zoom note**: Scope scales only `MinAimFOVReduction`/`MaxAimFOVReduction`, snapshotting the original values per ranged-weapon pointer. Restore must write the cached vanilla values back when RMB is released, weapon changes, mod disables, or no ranged weapon is equipped.

### ViewmodelAvatar (`Il2CppScheduleOne.PlayerScripts`)
- Methods: `SetVisibility(bool)`, `SetOffset(Vector3)`, `SetRotationOffset(Vector3)`, `SetAnimatorController(RuntimeAnimatorController)`, `SetAppearance(AvatarSettings)`.
- Fields/properties: `IsVisible`, `ArmShift`, `ParentAvatar`, `Animator`, `Avatar`, `RightHandContainer`.
- **Scope gotcha**: `SetVisibility(false)` hides the viewmodel via game API but does not necessarily imply weapon aim/fire state; call it only after the actual ranged weapon `Aim` value starts rising.
- **2026-05 Scope runtime gotcha**: `SetVisibility(false)` was observed to break Aim+Fire behavior in the Scope mod. Avoid using it for active ADS hiding if firing must remain vanilla.
- **2026-05 Scope runtime gotcha**: Large `SetOffset(...)` hiding (e.g. moving the viewmodel far below the camera) allowed Aim+Fire to work again, but damaged vanilla animations and MoreGuns/Minigun windup sound/state. Treat offset-hiding as unsafe for active weapons.
- **Current safe Scope policy**: leave `ViewmodelAvatar` untouched while aiming. If the viewmodel must be hidden later, prefer investigating selective renderer/mesh visibility that does not change `ViewmodelAvatar.IsVisible`, offsets, GameObject active state, or animator state.

### ReticleController (`Il2CppScheduleOne.Combat`)
- Used from `HUD.Instance._reticleController`.
- Methods: `ShowReticle(float duration = -1f)`, `HideReticle(float duration = -1f)`, `SetReticle(float spreadAngle)`.
- **Scope note**: Vanilla may hide the reticle during ADS because iron sights are normally visible. If the mod hides or visually replaces the viewmodel/iron sights, call `ShowReticle(-1f)` every frame while aiming; doing it in both `OnUpdate` and `OnLateUpdate` helps when vanilla or another mod hides it later in the frame.
- **Open issue**: In runtime tests, AK47 reticle behaved, but Minigun/Pistol reticle could still be missing. If `ShowReticle(-1f)` is insufficient, inspect `ReticleUI`/`ReticleLineUI` alpha/scale/CanvasGroup directly.

### ItemDefinition identity (`Il2CppScheduleOne.ItemFramework` / S1API wrapper)
- Native `ItemDefinition.name` is a display/runtime name and may not match stable IDs. Runtime Scope logs showed gold pistol as `M1911 (Gold)`, not `M1911_Gold`.
- S1API wrapper exposes stable identity semantics (`ID`, `Name`, `GUID => ID`) in `S1API.Items.ItemDefinition`.
- **Scope gotcha**: Do not build per-weapon config keys from `ItemDefinition.name` if variants like M1911 vs M1911 Gold must be distinguished. Use a stable item ID/GUID source where possible.

### GameInput (`Il2CppScheduleOne`)
- Singleton-like: MonoBehaviour on player, uses Unity `PlayerInput` for Input System callbacks
- **Exit/Back flow**: `OnBack()` (Input System callback for "Back" action = RMB / Gamepad B) → `Exit(ExitType.RightClick)` → iterates `exitListeners` by priority → creates `ExitAction` → calls registered `ExitDelegate`s
- Methods: `OnBack()` — private, Input System callback. Triggered by right mouse button / gamepad B
- Methods: `Exit(ExitType type)` — private, iterates `exitListeners`, creates `ExitAction(exitType=type)`, invokes delegates until `ExitAction.used == true`
- Methods: `ExitAll()` — public, closes all exit layers (10 callers in game)
- Methods: `OnEscape()` — Input System callback for ESC key. Separate binding from `OnTogglePauseMenu`
- Methods: `OnTogglePauseMenu()` — Input System callback for ESC key. Separate binding from `OnEscape`
- Static Methods: `RegisterExitListener(ExitDelegate listener, int priority = 0)` — registers callback for exit events. Higher priority = called first
- Static Methods: `DeregisterExitListener(ExitDelegate listener)` — removes callback
- Static Methods: `GetButton(ButtonCode)` / `GetButtonDown(ButtonCode)` / `GetButtonUp(ButtonCode)` — poll button state
- Methods: `GetAction(ButtonCode code)` — returns `InputAction` for a `ButtonCode`
- Static Field: `exitListeners` — `List<ExitListener>` — priority-sorted listener list
- **Key insight**: `OnBack()` is the SOLE entry point for right-click-to-close behavior. It calls `Exit(ExitType.RightClick)`. ESC key calls `Exit(ExitType.Escape)` separately
- **Key insight**: Exit listeners are processed in priority order. First listener that calls `ExitAction.Use()` consumes the event — subsequent listeners see `used == true` and skip
- **⚠️ CRITICAL**: ESC fires **TWO** separate Unity Input System callbacks in the **same frame**: `OnEscape` AND `OnTogglePauseMenu`. They are independent bindings on the same key. Patching only `OnEscape` does NOT prevent `OnTogglePauseMenu` from opening the pause menu. Both must be patched. The game's coordination mechanism is `TogglePauseInputUsed` — set by `OnEscape`, checked by `OnTogglePauseMenu`. See il2cpp-rules.md → "Unity Input System Same-Frame Dual Callbacks" for the fix pattern
- Static Field: `TogglePauseInputUsed` (bool) — coordination flag between `OnEscape` and `OnTogglePauseMenu`. `OnEscape` sets it `true`; `OnTogglePauseMenu` checks it and skips if `true`, then resets to `false`. **Must be set by Harmony prefixes that block original OnEscape** (return false) to preserve the native coordination

### GameInput.ButtonCode (Nested Enum in `Il2CppScheduleOne.GameInput`)
- Values: `PrimaryClick` (0), `SecondaryClick` (1), `TertiaryClick` (2), `Forward`, `Backward`, `Left`, `Right`, `Jump`, `Crouch`, `Sprint`, `Escape`, `Back`, `Interact`, `Submit`, `TogglePhone`, `VehicleToggleLights`, `VehicleHandbrake`, `RotateLeft`, `RotateRight`, `ManagementMode`, `OpenMap`, `OpenJournal`, `OpenTexts`, `QuickMove`, `ToggleFlashlight`, `ViewAvatar`, `Reload`, `InventoryLeft`, `InventoryRight`, `Holster`, `VehicleResetCamera`, `SkateboardDismount`, `SkateboardMount`, `TogglePauseMenu`
- **Key insight**: `Back` = right mouse button / gamepad B. `Escape` = ESC key. These are SEPARATE button codes with separate flows

### GameInput.InputDeviceType (Nested Enum)
- Values: `KeyboardMouse` (0), `Gamepad` (1)

### GameInput.ExitListener (Nested Class)
- Fields: `listenerFunction` (ExitDelegate), `priority` (int)
- Higher priority listeners are called first in the exit chain

### GameInput.ExitDelegate (Nested Delegate)
- Signature: `void ExitDelegate(ExitAction action)`
- Called by `GameInput.Exit()` with the created `ExitAction`

### ExitType (`Il2CppScheduleOne.DevUtilities`)
- Enum: `RightClick` (0), `Escape` (1)
- Passed to `GameInput.Exit()` and stored in `ExitAction.exitType`

### ExitAction (`Il2CppScheduleOne.DevUtilities`)
- Properties: `exitType` (ExitType) — which input triggered the exit
- Properties: `used` (bool, get/set) — whether the exit was consumed
- Properties: `Used` (bool, get/set) — same as `used` (property wrapper)
- Methods: `Use()` — sets `used = true`, marks event as consumed
- **Key insight**: Listeners check `exitAction.used` — if already `true`, they skip. Call `Use()` to consume the event and prevent lower-priority listeners from acting

### Phone App Exit Flow (Complete Chain)
```
User right-clicks (or presses Gamepad B)
  ↓
Unity Input System → GameInput.OnBack()
  ↓
GameInput.Exit(ExitType.RightClick)
  ↓
Iterates exitListeners (sorted by priority)
  ↓
ExitDelegate(ExitAction { exitType = RightClick, used = false })
  ↓
App<T>.Exit(ExitAction exit)   ← virtual, on the currently open phone app
  ↓
exit.Use()  +  App<T>.SetOpen(false) / Close()
  ↓
Phone returns to HomeScreen
```

#### Custom Phone App CloseApp Pattern (Mods)
Custom phone apps (Snorify, Drones) that bypass the exit listener system use a manual `CloseApp()` method:
```
CloseApp()
  ↓ AppsCanvas.SetIsOpen(false)
  ↓ HomeScreen.SetIsOpen(true)
  ↓ Phone.ActiveApp = null
  ↓ phone.SetIsHorizontal(false)
  ↓ container.SetActive(false)
  ↓ GameInput.IsTyping = false
```
- **⚠️ DO NOT call `phone.SetIsOpen(false)` after `CloseApp()`** — CloseApp already transitions to HomeScreen. Adding `SetIsOpen(false)` closes the entire phone instead of returning to homescreen.
- The Exit Harmony prefix should `return false` after calling `CloseApp()` to suppress the original Exit method (which would invoke exit listeners on an already-closed app).

### App\<T\> (`Il2CppScheduleOne.UI`) — Generic Phone App Base Class
- Base class for ALL phone apps (JournalApp, MapApp, MessagesApp, DeliveryApp, etc.)
- Inherits from: `NetworkBehaviour` (via intermediate classes)
- Virtual Methods: `Exit(ExitAction exit)` — called by exit system. Default: checks `exitType`, calls `Use()` + `SetOpen(false)`
- Virtual Methods: `SetOpen(bool open)` — opens/closes the app UI
- Virtual Methods: `Update()` — per-frame tick
- Virtual Methods: `OnPhoneOpened()` — called when phone becomes visible
- Methods: `Close()` — private, internal close logic
- Methods: `ShortcutClicked()` — private, homescreen icon tap
- Methods: `SetNotificationCount(int count)` — notification badge
- Methods: `GenerateHomeScreenIcon()` — creates homescreen button
- Methods: `IsHoveringButton()` — private, UI hover check
- Properties: `isOpen` (bool, get/protected set)
- Fields: `Apps` (static list), `AppName` (string), `IconLabel` (string), `AppIcon` (Sprite)
- Fields: `Orientation` (EOrientation enum: Horizontal/Vertical), `AvailableInTutorial` (bool)
- Fields: `appContainer` (GameObject), `notificationContainer` (GameObject), `notificationText` (TMP_Text)
- Fields: `appIconButton` (Button) — homescreen icon button
- **Key insight**: To hook into phone app close on right-click, override `Exit(ExitAction)` in a subclass OR register an `ExitListener` with appropriate priority
- **Key insight**: `Exit()` receives the full `ExitAction` — mods can distinguish `RightClick` vs `Escape` and react differently
- **Gotcha**: `Close()` is private — call `SetOpen(false)` instead from mod code

### AppsCanvas (`Il2CppScheduleOne.UI.Phone`)
- Container for phone app canvases
- Properties: `isOpen` (bool, get/private set)
- Methods: `PhoneOpened()`, `PhoneClosed()` — lifecycle callbacks
- Methods: `SetIsOpen(bool o)` — public, sets open state
- Methods: `SetCanvasActive(bool)` — private, canvas visibility
- Methods: `DelayedSetCanvasActive(bool, float)` — coroutine for delayed canvas toggle

### Phone (`Il2CppScheduleOne.UI.Phone`)
- The phone container/controller
- Properties: `IsOpen` (bool), `isHorizontal` (bool), `isOpenable` (bool), `FlashlightOn` (bool)
- Fields: `closeApps` (Action) — invoked to close all apps
- Fields: `onPhoneClosed` (Action) — invoked when phone closes
- Methods: `RequestCloseApp()` — public, triggers app close from phone level
- Methods: `ToggleFlashlight()`, `SetOpenable(bool)`, `SetIsOpen(bool)`
- Methods: `Update()` — per-frame tick (virtual)

### Concrete Phone Apps (all extend App\<T\>)
| App Class | Namespace |
|-----------|-----------|
| `JournalApp` | `Il2CppScheduleOne.UI.Phone` |
| `MapApp` | `Il2CppScheduleOne.UI.Phone.Map` |
| `MessagesApp` | `Il2CppScheduleOne.UI.Phone.Messages` |
| `DeliveryApp` | `Il2CppScheduleOne.UI.Phone.Delivery` |
| `ContactsApp` | `Il2CppScheduleOne.UI.Phone.ContactsApp` |
| `ProductManagerApp` | `Il2CppScheduleOne.UI.Phone.ProductManagerApp` |
| `DealerManagementApp` | `Il2CppScheduleOne.UI.Phone.Messages` |

### Mod Usage Patterns

**Pattern 1: Custom Exit Listener (global)**
```csharp
// Register in OnSceneWasLoaded("Main")
GameInput.RegisterExitListener(
    (Action<ExitAction>)OnExit,
    priority: 10  // higher = called before default app handlers
);

void OnExit(ExitAction action)
{
    if (action.used) return; // already consumed
    if (action.exitType != ExitType.RightClick) return;
    if (!myCustomUIOpen) return;

    action.Use(); // consume — phone apps won't also close
    CloseMyUI();
}

// Deregister in cleanup
GameInput.DeregisterExitListener((Action<ExitAction>)OnExit);
```

**Pattern 2: Override Exit in App\<T\> subclass**
```csharp
// If inheriting from App<T> (S1API PhoneApp):
public override void Exit(ExitAction exit)
{
    if (exit.used) return;
    if (exit.exitType == ExitType.RightClick && hasSubMenu)
    {
        exit.Use();
        CloseSubMenu(); // don't close whole app, just sub-menu
        return;
    }
    base.Exit(exit); // default: close app
}
```

## Avatar / Mugshot APIs

### Avatar (`Il2CppScheduleOne.AvatarFramework`)
- Method: `GetMugshot(Action<Texture2D> callback)`
- **Key insight**: forces mugshot generation without needing the phone map to be opened first.
- **Usage pattern**: callback receives `Texture2D`; mods can create a `Sprite` from it and assign `npc.MugshotSprite` proactively.

### MugshotGenerator (`Il2CppScheduleOne.AvatarFramework`)
- Methods: `GenerateMugshot()` and `GenerateMugshot(AvatarSettings settings, bool fileToFile, Action<Texture2D> callback)`
- **Key insight**: vanilla/S1API mugshot generation ultimately routes through `MugshotGenerator`; requests are safe to queue before the map UI is visible.

### NPC (`S1API.Entities`)
- Static field: `All` — `public static readonly List<NPC>`
- Field: `S1NPC` — `internal readonly Il2CppScheduleOne.NPCs.NPC`
- Field: `IsCustomNPC` — `internal readonly bool`
- Properties: `FirstName`, `LastName`
- Method: `SendTextMessage(string message, Response[]? responses = null, float responseDelay = 1f, bool network = true)`
- Property: `Appearance` — `public NPCAppearance { get; private set; }`
- **Gotcha**: current S1API IL2CPP builds expose custom-NPC state via fields, not public properties. Compatibility reflection must support `FieldInfo` + non-public binding flags for `S1NPC` / `IsCustomNPC`.
- **Key insight**: `NPC.All` includes vanilla and modded contacts. A mod can pick random vanilla SMS senders by filtering `IsCustomNPC == false`, excluding its own NPC instance, and requiring usable `FirstName`/`LastName` data.

### StaticDoor / NPCEnterableBuilding (`Il2CppScheduleOne.Doors`, `Il2CppScheduleOne.Map`)
- `StaticDoor.AccessPoint` returns the transform NPCs use as the door approach/interaction point.
- `StaticDoor.Building` points to the owning `NPCEnterableBuilding`.

## NPC Schedule / Signal Movement APIs

### NPCAction (`Il2CppScheduleOne.NPCs.Schedules`)
- Fields/properties: `npc`, `schedule`, `StartTime`, `HasStarted`, `IsActive`, `movement`.
- Method: `SetDestination(Vector3 position, bool teleportIfFail = true)` — action-level helper around NPC movement destination assignment.
- Lifecycle methods: `Started()`, `LateStarted()`, `ActiveUpdate()`, `Interrupt()`, `Resume()`, `Skipped()`, `WalkCallback(NPCMovement.WalkResult result)`.
- **Key insight**: vanilla schedule movement is action-owned. Repeated external `NPCMovement.SetDestination(...)` calls can fight vanilla AI and force visible repaths; prefer a one-shot action/signal setup where possible.

### NPCSignal (`Il2CppScheduleOne.NPCs.Schedules`)
- Base signal fields/properties: `MaxDuration`, `StartedThisCycle`.
- Lifecycle methods: `ShouldStart()`, `Started()`, `LateStarted()`, `ActiveUpdate()`, `Interrupt()`, `MinPassed()`.
- **Key insight**: signals are `NPCAction` subclasses and carry their owning `npc` / `schedule` references through the base fields.

### NPCSignal_WalkToLocation (`Il2CppScheduleOne.NPCs.Schedules`)
- Fields/properties: `Destination` (`Transform`), `FaceDestinationDir` (`bool`), `DestinationThreshold` (`float`), `WarpIfSkipped` (`bool`).
- Methods: `Started()`, `ActiveUpdate()`, `Resume()`, `Skipped()`, `IsAtDestination()`, `WalkCallback(NPCMovement.WalkResult)`, `ReachedDestination()`.
- **Usage pattern**: set `Destination` to a `Transform` at the desired coordinate, set threshold/face/warp options, assign base `npc` and `schedule`, then call `Started()` once for a vanilla-style walk signal. Avoid repeated timed reapplication unless vanilla overrides the action.
- **Gotcha**: runtime-created FishNet/NetworkBehaviour schedule components can be fragile; when possible, reuse/precreate actions. For short-lived cleanup movement, a guarded one-shot signal with fallback direct movement is safer than mutating schedule lists.

### NPCScheduleManager (`Il2CppScheduleOne.NPCs`)
- Fields/properties: `ScheduleEnabled`, `CurfewModeEnabled`, `ActiveAction`, `PendingActions`, `Npc`, `ActionsAwaitingStart`, `ActionList`.
- Methods: `EnableSchedule()`, `DisableSchedule()`, `InitializeActions()`, `UpdateActions()`, `EnforceState()`, `EnforceState(bool)`, `SetCurfewModeEnabled(bool)`.
- **Key insight**: `StartAction(...)` exists but is private in the Il2Cpp wrapper. External mods should not rely on direct private schedule start unless using reflection/IL2CPP invoke deliberately.
- `StaticDoor.doorIndex` is the building-local door index.
- `NPCEnterableBuilding.GetClosestDoor(Vector3 pos, bool useableOnly)` returns the nearest door for a position.
- `NPC.CurrentBuilding` and `NPC.LastEnteredDoor` can be used to resolve a vanilla NPC's home/interior door at runtime.
- **Key insight**: For StorageUnit despawn targets, resolve Greg Figgle by `NPCManager.GetNPC("greg_fliggle")`, then use `greg.CurrentBuilding.GetClosestDoor(greg.transform.position, false)?.AccessPoint.position`, falling back to `greg.LastEnteredDoor` if needed. This avoids hardcoding uncertain interior-door coordinates.

### NPCAppearance (`S1API.Entities`)
- Method: `GenerateMugshot()` — `internal`. **Coroutine, not synchronous** — the call returns immediately, the mugshot only arrives seconds later in `npc.Icon` / `npc.MugshotSprite`
- Method: `Build()` calls `GenerateMugshot()` automatically.
- Internal flow: queues the appearance into `ProcessMugshotQueue()` and waits for `MugshotGenerator.MugshotRig` to exist.
- **Key insight**: S1API custom NPC mugshots are not updated by replacing PreemNav proxy visuals directly; S1API refreshes the real `NPCPoI` icon path.
- **PreemNav rule**: if a native `NPCPoI` exists, prefer it over a proxy so S1API `UpdatePoiIcons(...)` can update the visible mugshot.
- **Custom-Icon Gotcha**: If you set a **custom** Icon/Mugshot on a custom NPC (e.g., `npc.Icon = customSprite`), `GenerateMugshot()` will **overwrite** it seconds after spawn. Solution: either Harmony-patch the mugshot apply path, or use a polling enforcer in the `Update()` loop that re-sets the custom sprite every frame until stable.

### NPCPoI icon refresh (`S1API.Entities.NPCAppearance`)
- Internal method: `UpdatePoiIcons(Il2CppScheduleOne.NPCs.NPC npc, Sprite iconSprite)` — `private static`
- Behavior: scans `Object.FindObjectsOfType<NPCPoI>()`, finds matching `npcpoI.NPC`, then writes the sprite into `IconContainer/Outline/Icon`.
- **Gotcha**: the first parameter is the wrapped native game NPC, not the `S1API.Entities.NPC` wrapper.
- **Gotcha**: if a mod hides or replaces the native `NPCPoI` for an NPC, S1API mugshot updates will not reach the currently visible phone-map icon.

### PropertyManager (`Il2CppScheduleOne.Property`)
- Method: `GetProperty(string propertyCode)`
- **Key insight**: mods can resolve a vanilla property by code (for example `west_storage`) without scanning scene hierarchies manually.

### Property (`Il2CppScheduleOne.Property`)
- Static fields: `Properties`, `OwnedProperties`, `UnownedProperties`
- Property: `PropertyCode`
- Field/property: `PoI`
- Method: `SetOwned()`
- **Key insight**: a purchased property already has a native `PoI`; the minimal GPS/map-marker path is to resolve the property and reuse its existing `PoI` instead of creating a custom marker system.
- **Gotcha**: if property ownership is handled only in mod save data, call the native `SetOwned()` path as well when you want the vanilla property marker/UI state to become active.

### ItemInstance (`Il2CppScheduleOne.ItemFramework`)
- Base class for all item instances. **Abstract since 0.44.**
- Constructor: `ItemInstance(ItemDefinition definition, int quantity)` — **only constructor since 0.44** (parameterless ctor removed)
- Properties: `ID` (string), `Name` (string, virtual), `Quantity` (int), `Icon` (Sprite, virtual)
- Properties: `Definition` (ItemDefinition, read-only) — **was field `definition` pre-0.44, now read-only property**
- Methods: `GetCopy(int overrideQuantity = -1)` (**abstract since 0.44**), `GetItemData()` (virtual)
- Inheritance chain (0.44): `BaseItemInstance` → `ItemInstance` → `StorableItemInstance` → `QualityItemInstance` → `ProductItemInstance`
- **0.44 breaking change**: `EItemCategory` and `ELegalStatus` enums moved from `Il2CppScheduleOne.ItemFramework` to `Il2CppScheduleOne.Core.Items.Framework` (in `Il2CppScheduleOne.Core.dll`)

### ItemSlot (`Il2CppScheduleOne.ItemFramework`)
- Properties: `ItemInstance`, `Quantity`, `IsLocked`, `IsAtCapacity`, `SlotIndex`, `IsAddLocked`, `IsRemovalLocked`
- Properties: `HardFilters` (List\<ItemFilter\>), `PlayerFilter` (SlotFilter), `CanPlayerSetFilter` (bool)
- Methods: `SetStoredItem(ItemInstance, bool replicate)` — direct set, does NOT check HardFilters
- Methods: `InsertItem(ItemInstance)`, `AddItem(ItemInstance, bool)` — may check filters
- Methods: `ClearStoredInstance(bool replicate)` — removes item from slot
- Methods: `ChangeQuantity(int delta, bool replicate)`
- Methods: `ApplyLock(NetworkObject, string reason, bool)`, `RemoveLock(bool)`
- Methods: `AddFilter(ItemFilter)` — adds to HardFilters list
- Methods: `DoesItemMatchHardFilters(ItemInstance)` — checks all hard filters, returns bool
- Methods: `DoesItemMatchPlayerFilters(ItemInstance)`
- Methods: `GetCapacityForItem(ItemInstance, bool)` — checks filters + capacity
- Methods: `SetIsAddLocked(bool)`, `SetIsRemovalLocked(bool)`

### ItemFilter (`Il2CppScheduleOne.ItemFramework`)
- Base class for slot filters
- Virtual: `DoesItemMatchFilter(ItemInstance)` — override to create custom filters
- Used as HardFilter via `ItemSlot.AddFilter()`
- Game checks HardFilters in `GetCapacityForItem` / employee routing — NOT in `SetStoredItem`

### MapPositionUtility (`Il2CppScheduleOne.Map`) extends Singleton\<MapPositionUtility\>
- Fields: `OriginPoint` (Transform), `EdgePoint` (Transform), `MapDimensions` (Vector2), `conversionFactor` (float, auto-property)
- Methods: `GetMapPosition(Vector3 worldPosition)` → `Vector2` — converts world coordinates to map-pixel coordinates
- Methods: `Recalculate()` — recalculates `conversionFactor` from OriginPoint/EdgePoint
- Methods: `Awake()` — calls `Recalculate()` on init
- **Key insight**: `conversionFactor` is calculated from the distance between `OriginPoint` and `EdgePoint` relative to `MapDimensions`. All map-position calculations in the game go through this utility.
- **Usage**: `Singleton<MapPositionUtility>.Instance.GetMapPosition(npc.transform.position)` for NPC map coordinates. Used by vanilla `POI.UpdatePosition()`, PreemNav proxies, and minimap markers.
- **Gotcha**: Returns stale data if `MapPositionUtility` singleton is null (scene transition). Always null-check the instance.

## Registry & Utilities

### PauseMenu (`Il2CppScheduleOne.UI`)
- Inherits from `Singleton<PauseMenu>`
- Fields / properties: `Canvas`, `Container` (`RectTransform`), `Screen`, `FeedbackForm`, `uiScreen`, `uiPanel`, `onPause`, `onResume`
- Methods: `Pause()`, `Resume()`, `StuckButtonClicked()`, `DelayPanelSelect()`
- **Key insight**: `Container` is the stable pause-menu UI anchor for injected buttons.
- **Gotcha**: pause-menu buttons are not guaranteed to be direct children of `Container`; recursive button lookup from `Container` is safer than checking only immediate children by name.

### Registry (`Il2CppScheduleOne`)
- Methods: `GetItem(string id)` — returns `ItemDefinition` by string ID (e.g., `"baggies"`)
- **Key insight**: Returns null if item not found. Used to look up packaging definitions by ID
- **Registering custom ProductDefinitions**: Mod-owned `ProductDefinition` clones (e.g., `ogkush` clones for custom product IDs) MUST be registered in the `Registry` **before** `ProductManagerLoader.Load` runs (see entry below). Otherwise vanilla spams `SetProductListed: product is not found` and the `ProductManager` state becomes inconsistent — resulting in NREs in downstream customer paths (`Customer.PlayerAcceptedContract`, `ActionList.InvokeAllStaggered`).

### NotificationsManager (`Il2CppScheduleOne.UI`)
- Singleton: `NotificationsManager.Instance`
- Methods: `SendNotification(string title, string subtitle, Sprite icon, float duration, bool playSound)` — shows in-game notification popup
- **Key insight**: `duration` in seconds (e.g., 5f). `playSound=true` for audio feedback. Used by Drones mod for delivery-complete notifications

### ItemDeserializer (`Il2CppScheduleOne.Persistence`)
- Methods: `LoadItem(string json)` — deserializes a JSON string (from `ItemInstance.GetItemData()` → `JsonUtility.ToJson()`) back into an `ItemInstance`
- **Key insight**: Used in save/load pipelines. Returns the concrete subtype (`ProductItemInstance`, etc.) based on the serialized data

### ProductManagerLoader (`Il2CppScheduleOne.Persistence.Loaders`)
- Methods: `Load(string mainPath)` — vanilla loader for `ProductManager` state from save (`DiscoveredProducts`, `ListedProducts`, `ProductPrices`, Contract-Receipts)
- **Runtime order**: runs **before** most S1API `Saveable` loaders in the save-load pipeline
- **CRITICAL for mods with custom ProductDefinitions**: Mod products (e.g., `bp_escort_*`) must be registered in the vanilla `Registry` BEFORE this loader runs. Race symptom: `Latest.log` shows `SetProductListed: product is not found (<id>)`, followed by `IndexOutOfRange` in `ActionList.InvokeAllStaggered`, then NRE wall in `Customer.PlayerAcceptedContract` when the player selects a time slot in `DealWindowSelector`
- **Proven Pattern (bigpimpin CHANGE-266)**: Harmony-Prefix on `ProductManagerLoader.Load(string)` calls the mod's own registry init (e.g., `EscortProductRegistry.RegisterAll()`) directly before the vanilla load. Init must be **idempotent + fail-open**. Keep an Update-loop timer fallback as safety net in case the Registry / ogkush template isn't ready at prefix time

### ProductItemInstance (`Il2CppScheduleOne.Product`) extends QualityItemInstance
- Properties: `AppliedPackaging` (PackagingDefinition, can be null for unpackaged)
- Properties: `Amount` (int — units per package, e.g., 1 for baggie, 5 for jar, 20 for brick)
- Fields: `PackagingID` (string), `packaging` (internal field — prefer `AppliedPackaging` property)
- Methods: `SetPackaging(PackagingDefinition)`, `SetQuality(EQuality)`
- Methods: `GetMonetaryValue()`, `GetTotalAmount()`, `GetSimilarity(ProductDefinition, EQuality)`
- **Packaging null check**: `AppliedPackaging == null` means unpackaged/raw product
- **TryCast pattern**: Always use `item.TryCast<ProductItemInstance>()` — returns null for non-product items

### PackagingDefinition (`Il2CppScheduleOne.Product.Packaging`) extends StorableItemDefinition
- Properties: `Quantity` (int — units per package), `StealthLevel` (EStealthLevel)
- Properties: `FunctionalPackaging`, `Equippable_Filled`, `StoredItem_Filled`
- Packaging types by Quantity: Baggie (1), Jar (5), Brick (20)

### StorageEntity (`Il2CppScheduleOne.Storage`)
- Properties: `ItemSlots` (Il2Cpp List\<ItemSlot\>), `SlotCount`, `DisplayRowCount`, `StorageEntityName`
- Methods: `GetAllItems()` — returns all non-null ItemInstances

### PlaceableStorageEntity (`Il2CppScheduleOne.ObjectScripts`) extends GridItem
- Properties: `StorageEntity`, `InputSlots` (List\<ItemSlot\>), `OutputSlots` (List\<ItemSlot\>)
- Properties: `IsAcceptingItems` (bool), `Configuration` (EntityConfiguration)
- Properties: `NPCUserObject`, `PlayerUserObject` (NetworkObject)
- Methods: `Start()` — **heavy init** (17 Xrefs in 0.45f1). Wires `StorageEntityInteractable.StorageEntity`, populates Input/OutputSlots. Only runs on scene-instantiated objects (NOT on prefabs from Resources)
- Methods: `Awake()` — lighter init, does NOT wire SEI.StorageEntity
- Implements `ITransitEntity` interface — used by employee/handler routing system

### StorageEntityInteractable (`Il2CppScheduleOne.Storage`)
- Properties: `StorageEntity`, `message` (string — displayed name)
- Properties: `LimitInteractionAngle`, `interactionState`, `interactionType`
- Methods: `Awake()` (private, 4 Xrefs — minimal init, does NOT set StorageEntity)
- **⚠️ 0.45f1 CRITICAL**: `StorageEntity` field is NOT wired on prefabs. Set at runtime by `PlaceableStorageEntity.Start()`. Prefabs from `Resources.FindObjectsOfTypeAll` always have null `StorageEntity`. Use 3-path fallback: SEI.StorageEntity → `GetComponent<StorageEntity>()` → `GetComponentInChildren<StorageEntity>(true)`
- **`message`**: The in-world interaction label (e.g., `[E] dealer`). Must be updated manually when renaming — game clipboard only updates `EntityConfiguration.Name`

### EntityConfiguration (`Il2CppScheduleOne.Management`)
- Properties: `Name` (ConfigField — wraps the display name set by clipboard)
- `Name.Value` (string) — the current name
- `Name.SetValue(string, bool)` — sets the name
- **Key insight**: Clipboard rename ONLY updates `EntityConfiguration.Name.Value`. Does NOT propagate to `StorageEntityName` or `StorageEntityInteractable.message` — mod must sync via `SyncNameFromEntityConfiguration()`

### ITransitEntity (`Il2CppScheduleOne.Management`)
- Interface for employee routing system
- Properties: `InputSlots`, `OutputSlots`, `AccessPoints`, `IsAcceptingItems`, `Selectable`
- Methods: `GetInputCapacityForItem(ItemInstance, NPC, bool)` — checks HardFilters
- Methods: `ReserveInputSlotsForItem(ItemInstance, NetworkObject)` — locks slots for delivery
- Methods: `InsertItemIntoInput(ItemInstance, NPC)` — places item into input slot
- Methods: `RemoveSlotLocks(NetworkObject)`
- **Key insight**: Employee handler calls `GetInputCapacityForItem` first → if HardFilter rejects → station not a valid target

### Contract (`Il2CppScheduleOne.Quests`)
- Properties: `ProductList`, `Customer`, `Dealer`, `Payment`, `DeliveryWindow`, `DeliveryLocation`
- Properties: `State` (enum: 1 = Active), `hudUI`

### Dealer (`Il2CppScheduleOne.Economy`)
- Extends: `NetworkBehaviour`
- Properties: `IsRecruited` (bool), `ActiveContracts` (Il2CppSystem.Collections.Generic.List\<Contract\>), `fullName` (string)
- Properties: `NetworkObject` (NetworkObject) — used as parameter in `RpcLogic___ProcessHandoverServerSide`
- Methods: `GetAllSlots()` — returns all inventory slots
- Methods: `AddItemToInventory(ItemInstance)` — adds item to dealer's inventory (used by drone dealer delivery)
- Static: `AllPlayerDealers` (Il2CppSystem.Collections.Generic.List\<Dealer\>) — all recruited dealers
- **Key insight**: Drone dealer delivery iterates dealer inventory to determine restock needs, then delivers products via `AddItemToInventory()`. Works for both vanilla and S1API dealers

### Customer (`Il2CppScheduleOne.Economy`)
- Extends: `NetworkBehaviour`
- Properties: `CurrentContract` — active deal contract (null = no active deal)
- Properties: `DropOffLocation` (Transform) — delivery location
- Properties: `IsAwaitingDelivery` (bool) — waiting for delivery
- Properties: `NPC` (NPC) — the NPC reference for this customer
- Properties: `AssignedDealer` (Dealer) — dealer assigned to this customer
- Properties: `CompletedDeliveries` (int, get/set) — delivery counter
- Static: `UnlockedCustomers` — `Il2CppSystem.Collections.Generic.List<Customer>` — all unlocked customers
- Reached via `Contract.Customer`
- Methods: `ProcessHandover(HandoverScreen.EHandoverOutcome, Contract, List<ItemInstance>, bool, bool)` — normal game handover via ServerRpc. May fail silently due to Il2Cpp item serialization
- Methods: `IsReadyForHandover(bool)` — checks if customer is at delivery location and ready
- Methods: `EvaluateDelivery(Contract, List<ItemInstance>, out float, out EDrugType, out int, out float)` — evaluates delivery quality, returns satisfaction float
- Methods: `RpcLogic___ProcessHandoverServerSide_3760244802(EHandoverOutcome, List<ItemInstance>, bool, float, ProductList, float, NetworkObject)` — direct server-side RPC logic bypass (no FishNet serialization). Executes full handover including payment, addiction, affinity, recommendations
- Methods: `CurrentContractEnded(EQuestState)` — notifies customer that contract ended
- Methods: `ChangeAddiction(float)` — adjusts addiction level
- Methods: `AdjustAffinity(EDrugType, float)` — adjusts product type affinity based on satisfaction
- Methods: `ContractWellReceived(string)` — triggers NPC recommendation chain (satisfaction ≥ 0.8)
- **Proven pattern** (Drones mod): 4-level handover fallback chain: (1) ProcessHandover (normal RPC), (2) RpcLogic bypass (direct native call), (3) Manual (individual API calls), (4) LastResort (SubmitPayment+Complete only)
- **Proven pattern** (IllegalRave): `IsNPCBusyWithDeal()`: Skip NPCs with `CurrentContract != null || IsAwaitingDelivery`. `BlockCustomerDeals()`: Set `enabled = false` on Customer for rave NPCs. `UnblockCustomerDeals()`: Re-enable on cleanup (with WasCollected safety).
- **Gotcha**: Customer extends NetworkBehaviour. `.enabled = false` stops all NetworkBehaviour processing including deal assignment. Must re-enable on cleanup.

### Contract (`Il2CppScheduleOne.Quests`)
- Properties: `ProductList` (ProductList), `Customer` (GameObject), `Dealer` (Dealer), `Payment` (float), `DeliveryWindow`, `DeliveryLocation` (Transform)
- Properties: `State` (enum: 1 = Active), `hudUI` (GameObject — in-world HUD element)
- Methods: `SubmitPayment(float bonus)` — submits payment to player (bonus added to base payment)
- Methods: `Complete(bool replicate)` — marks contract as completed

### ProductList (`Il2CppScheduleOne.Quests`)
- Properties: `entries` (Il2CppSystem.Collections.Generic.List\<Entry\>)
- Nested class `Entry`: `ProductID` (string), `Quantity` (int)
- **Key insight**: Iterated to determine which products and quantities a contract requires

### HandoverScreen (`Il2CppScheduleOne.UI.Handover`)
- Enum: `EHandoverOutcome` — outcome of handover. Value `1` = successful delivery
- Used as parameter in `Customer.ProcessHandover()` and `RpcLogic___ProcessHandoverServerSide`

### PlayerInventory (`Il2CppScheduleOne.PlayerScripts`)
- Singleton: `PlayerInventory.Instance`
- Field: `Player.Inventory` — returns only a **subset** of slots (Hotbar). Good for hotbar-only checks, but NOT for full inventory scans
- Methods: `GetAllInventorySlots()` — **only reliable source** for the complete player inventory (hotbar + backpack slots). Returns `Il2CppSystem.Collections.Generic.List<ItemSlot>`
- **Proven Pattern**: Drone/Storage/DeadDrop mods that need to search for specific items in the player inventory (e.g., Coke baggies, Cash, Tools) MUST use `GetAllInventorySlots()` — otherwise items in the backpack are missed (bigpimpin DeadDrop-Monitor, Drones Inventory-Helper)

### MoneyManager (`Il2CppScheduleOne.Money`)
- Properties: `cashBalance` (float, get) — current cash
- Properties: `onlineBalance` (float, SyncVar) — online bank balance
- Properties: `cashInstance` (CashInstance)
- Properties: `LastCalculatedNetworth` (float, get/set)
- Properties: `lifetimeEarnings` (float, SyncVar)
- Properties: `ledger` (List\<Transaction\>) — transaction log
- Properties: `CashSound` (AudioSourceController) — cash sound effect
- Methods: `ChangeCashBalance(float change, bool visualize, bool playCashSound)` — add/subtract cash
- Methods: `CreateOnlineTransaction(string name, float unitAmount, float quantity, string note)` — online transaction
- Methods: `ReceiveOnlineTransaction(string label, float amount)` — adds money to player balance
- Methods: `GetNetWorth()` (float) — calculate net worth
- Static singleton access
- **Key insight**: For mod betting systems, use `ChangeCashBalance(-amount)` to deduct and `ChangeCashBalance(+payout)` for winnings. Set `visualize=true` for visual feedback, `playCashSound=true` for audio

## Type Hierarchy: Items

```
ItemInstance (base)
  ├─ QualityItemInstance  (has quality, NO packaging)
  │     └─ ProductItemInstance  (has quality + packaging)
  └─ other item types
```

- **Critical**: `QualityItemInstance` that is NOT a `ProductItemInstance` has quality but no packaging info
- **Hard-casting** a `QualityItemInstance` to `ProductItemInstance` throws `InvalidCastException`
- **Rule**: Always use `TryCast<ProductItemInstance>()` — returns null for non-product items
- Base class properties (`ItemInstance.ID`, `ItemInstance.Quantity`) should be used where `ProductItemInstance`-specific properties (packaging, Amount) aren't needed

### GUIDManager (`Il2CppScheduleOne.Management`)
- Methods: `RegisterObject(IGUIDRegisterable, GameObject)` — registers object for GUID-based lookup
- Methods: `GetObject<T>(string guid)` — resolves object by GUID string
- Methods: `DeregisterObject(IGUIDRegisterable)` — removes from registry
- **Key insight**: `SetGUID()` on a component only sets the GUID field. Registration requires explicit `RegisterObject()` call (normally done in `InitializeGridItem`/`OnSpawnServer` which custom objects bypass)
- **Key insight**: `ObjectField.Load()` resolves destinations via `GUIDManager.GetObject<BuildableItem>()` — returns null for unregistered GUIDs
- **Deregistration**: Must be called in both `Destroy()` and `Cleanup()` for reload safety

### IGUIDRegisterable (`Il2CppScheduleOne.Management`)
- Interface: implemented by objects that can be registered with GUIDManager
- Cast pattern: `component.Cast<IGUIDRegisterable>()` (safe — known interface implementation)

### ManagementClipboard_Equippable (`Il2CppScheduleOne.Management`)
- Properties: `SelectionInfoUI`, `CurrentConfigurables` (list)
- Methods: `Update()` — runs per-frame when clipboard is equipped
- **Key insight**: When clipboard is equipped and player looks at a configurable, `Update()` sets `CurrentConfigurables` and `SelectionInfoUI`. When looking away, vanilla deselection path uses SyncVar-dependent operations that silently no-op on non-networked objects → text/outlines can stick

### ManagementWorldspaceCanvas (`Il2CppScheduleOne.Management.UI`)
- Properties: `ShownConfigurables`, `SelectedConfigurables` (lists), `HoveredConfigurable`, `OutlinedConfigurable` (single refs)
- Methods: `UpdateUIs()` — iterates ShownConfigurables, accesses each `WorldspaceUI` property
- Methods: `UpdateSelection()` — manages outline transitions (hover A → hover B → hide A outline)
- Called from `Update()`: `UpdateUIs()` first, then `UpdateSelection()`
- **Key insight**: `HoveredConfigurable`/`OutlinedConfigurable` must NOT be nulled by patches — `UpdateSelection()` needs them for outline transition detection. Only filter `ShownConfigurables`/`SelectedConfigurables`

### ConfigurationReplicator (`Il2CppScheduleOne.Management`)
- Methods: `ReplicateField(ConfigField)` — serializes field value via FishNet RPC
- **Key insight**: Uses FishNet RPCs internally. `NetworkBehaviour` references are serialized by `NetworkObject.ObjectId`. Non-networked objects (no `NetworkObject`) serialize as null → RPC response overwrites local value back to null

### ObjectField (`Il2CppScheduleOne.Management`)
- Methods: `SetObject(BuildableItem obj, bool network)` — sets the referenced object
- Methods: `Load(string guidString)` — resolves via `GUIDManager.GetObject<BuildableItem>()`
- Methods: `GetData()` — serializes as GUID string
- **Key insight**: `SetObject(obj, network: true)` triggers `ConfigurationReplicator.ReplicateField` → FishNet RPC roundtrip. For non-networked targets, force `network = false` to prevent null-overwrite

### RouteEntryUI (`Il2CppScheduleOne.UI.Management`)
- MonoBehaviour: renders a single route entry in the clipboard route list
- Properties: `AssignedRoute` (AdvancedTransitRoute), `SourceLabel` (TMP_Text), `SourceIcon` (Image), `DestinationLabel` (TMP_Text), `DestinationIcon` (Image)
- Methods: `RefreshUI()` — refreshes display from route data. Accesses `ITransitEntity` properties (Name, Icon) on source/destination
- **Gotcha**: When source/destination is a drone station PSE (no NetworkObject), SyncVar-dependent property access NullRefs. Finalizer suppression required

### AdvancedTransitRoute (`Il2CppScheduleOne.EntityFramework`)
- Represents a configured route between two `ITransitEntity` objects (e.g., PackagingStation → StorageRack)
- Fields: `_Source_k__BackingField` (ITransitEntity), `_Destination_k__BackingField` (ITransitEntity)
- **Key insight**: Backing fields accessed directly by native code. Managed property getters may not be called

## Grid / Building System

### GridItem (`Il2CppScheduleOne.ObjectScripts`) extends NetworkBehaviour
- Properties: `NetworkObject`, `GUID`, `gameObject`, `transform`
- Methods: `SetGUID(string)` — sets GUID field (does NOT register with GUIDManager)
- Methods: `InitializeGridItem()` — normal init path, registers with GUIDManager
- **Key insight**: Custom-spawned GridItems that bypass `InitializeGridItem` must manually call `GUIDManager.RegisterObject()`

### BuildableItem (`Il2CppScheduleOne.ObjectScripts`) extends GridItem
- Properties: `OutlineEffect` — visual outline component for clipboard selection (`Outlinable` from EPOOutline)
- Properties: `BuildPoint` (Transform) — the build/placement point used for tile-based positioning
- Used as target type for `ObjectField` / destination selection in clipboard

### Outlinable (`Il2CppEPOOutline`)
- Third-party outline component used by the game for clipboard selection highlighting
- Properties: `enabled` (bool) — toggle outline visibility
- **Key insight**: Cloned storage rack templates carry multiple `Outlinable` children. Keep one (disabled) for clipboard compatibility, destroy extras with `DestroyImmediate`

### Property (`Il2CppScheduleOne.Property`)
- Properties: `PropertyName` (string), `OwnedTiles` (list)
- Methods: tile-based placement system
- Grid paths define walkable areas for NPCs and placement validity

### FootprintTile (`Il2CppScheduleOne.Tiles`)
- Component on tiles that mark occupied building footprints
- **Key insight**: Must be destroyed on cloned storage racks to prevent tile conflicts. Use `GetComponentsInChildren<FootprintTile>(true)` + `DestroyImmediate`

### IndoorTile (`Il2CppScheduleOne.Tiles`)
- Component marking tiles as indoor (e.g., inside warehouses)
- Used by DroneStation to detect enclosed property interiors for vertical liftoff configuration

### NetworkObserver (`Il2CppFishNet.Observing`)
- FishNet network observer component for controlling network visibility
- **Key insight**: Must be destroyed on cloned storage racks — non-networked custom objects don't need network observation and it can cause errors

## Player / Save System

### Player (`Il2CppScheduleOne.PlayerScripts`)
- Methods: `WriteData(string folderPath)` — called by SaveManager on save events
- Methods: `Load()` — called on game load
- Inner class: `Player.Loader` — handles loading subfiles
- Methods: `Loader.TryLoadFile(string name, out string content)` — reads save subfile
- Methods: `WriteSubfile(string name, string content)` — writes named subfile to save folder
- **Key insight**: `WriteData()` is called with `[HarmonyWrapSafe]` — exceptions are silently caught. Mod patches must use explicit try/catch BEFORE HarmonyWrapSafe eats errors

### SaveManager (`Il2CppScheduleOne.Persistence`)
- Field: `SAVES_PER_FRAME` — processes saves incrementally over multiple frames
- **Key insight**: `Player.WriteData()` may fire in a later frame when Unity has already destroyed property GameObjects. Save code must handle destroyed objects gracefully
- **Save triggers**: Only fires on explicit events: sleep/day change, Quick Save mod, Save & Exit. Short sessions without triggers produce zero save events

## FishNet / Networking

### NetworkBehaviour (`FishNet.Object`)
- Properties: `NetworkObject` — the network identity component
- **SyncVar behavior**: SyncVar setters/getters only work with valid `NetworkObject`. Without it, set operations silently no-op
- **Key insight**: Direct field writes (`__field = value`) bypass SyncVar validation. Use for non-networked objects that inherit from types with SyncVars

### NetworkObject (`FishNet.Object`)
- Properties: `ObjectId` — unique network identity
- **Key insight**: FishNet serializes `NetworkBehaviour` references by `ObjectId`, NOT by GUID. Objects without `NetworkObject` serialize as null in FishNet RPCs

### NPC (`Il2CppScheduleOne.NPCs`)
- Properties: `FirstName` (string), `LastName` (string), `fullName` (string), `hasLastName` (bool)
- Properties: `ID` (string) — unique ID (e.g., `"igor_romanovich"`)
- Properties: `GUID` (Guid) — network GUID
- Properties: `BakedGUID` (string) — baked scene GUID for vanilla NPCs; custom/S1API NPCs may have empty or null values
- Properties: `IsImportant` (bool) — important NPCs respawn sooner / follow important-character handling
- Properties: `isVisible` (bool) - current visibility state
- Properties: `IsCustomNPC` (bool) - marks S1API NPCs from any mod (not just specific ones)
- Properties: `IsConscious` (bool), `IsInVehicle` (bool), `IsCurrentlyTargetable` (bool)
- Properties: `Movement` -> `NPCMovement` - `PauseMovement()`, `ResumeMovement()`, `SetAgentEnabled(bool)`
- Properties: `Behaviour` -> `NPCBehaviour` - `.enabled = false` prevents AI Update/OnTick
- Properties: `Avatar` -> `Avatar` - `.EmotionManager` for face expressions, `.GetMugshot(Action<Texture2D>)` for proactive mugshot generation
- Properties: `MugshotSprite` -> `Sprite` (field, get/set) — cached mugshot. Null until generated. Set by `Avatar.GetMugshot` callback or game's lazy init (MapApp open)
- Properties: `Health` -> `NPCHealth` — health/damage/KO system
- Properties: `Awareness` -> `NPCAwareness` — perception system
- Properties: `DialogueHandler` -> `DialogueHandler` — dialog handler
- Properties: `intObj` -> `InteractableObject` — interaction component on NPC
- Properties: `Responses` -> `NPCResponses`, `Actions` -> `NPCActions`
- Methods: `SetVisible(bool visible, bool notify)` - Force visibility state. Keep notify=false to avoid side effects
- Methods: `SetAnimationTrigger(string)` - Local-only trigger, CallerCount=4 in game code. Safe without network ownership
- Methods: `SetAnimationBool(string, bool)` - Local-only animation bool. Safe
- Methods: `TryCast<T>()` - Il2Cpp type cast (use to filter Dealers, PoliceOfficers)
- Methods: `SendImpact(Impact)` — virtual, send impact to NPC
- Methods: `ReceiveImpact(Impact)` — virtual, process incoming impact
- Methods: `OnKnockedOut()` — virtual KO callback
- Methods: `OnDie()` — virtual death callback
- Methods: `SetScale(float)` — set NPC scale
- Methods: `Hovered()` — virtual hover callback (called by game)
- Methods: `Interacted()` — virtual interaction callback (called by game)
- Methods: `OverrideAggression(float)` / `ResetAggression()` — aggression control
- Methods: `SendWorldSpaceDialogue(string text, float duration)` — worldspace bubble text
- **Gotcha**: `SendAnimationTrigger(string)` - FishNet Server RPC, do NOT call without ownership. Use `SetAnimationTrigger` instead
- **Gotcha**: `SetVisible(true, false)` can re-enable `NPCAnimation`. Must re-disable every frame
- **Gotcha**: `BakedGUID` may help distinguish vanilla NPCs from custom/S1API NPCs, but this heuristic is not fully verified and should not be the primary filter.
- **Key insight**: For third-party mod compatibility, prefer automatic/transparent via vanilla game lists
- **Key insight**: All named NPCs (Albert, Genghis, Igor, etc.) are individual classes inheriting from NPC
- **Key insight**: No `SpawnNPC` method exists — all NPCs pre-exist in world, access via `NPCManager.NPCRegistry`

### NPCBehaviour (`Il2CppScheduleOne.NPCs.Behaviour`)
- Properties: `CombatBehaviour` -> `CombatBehaviour` (direct field access)
- Properties: `FaceTargetBehaviour` -> `FaceTargetBehaviour`
- Properties: `behaviourStack` -> Stack of all behaviours
- Properties: `enabledBehaviours` -> List of active behaviours
- Methods: `GetBehaviour<T>()` — generic behaviour lookup
- Methods: `GetBehaviour(string name)` — behaviour by name

### NPCManager (`Il2CppScheduleOne.NPCs`) extends `NetworkSingleton<NPCManager>`
- Properties: `NPCRegistry` -> `Il2CppSystem.Collections.Generic.List<NPC>` (static) — **ALL registered NPCs** including inactive (inside buildings). Use this instead of `FindObjectsOfType<NPC>()` which only finds active GameObjects.
- Properties: `NPCWarpPoints` — collection of Transform warp points in the world
- Properties: `NPCContainer` — container Transform for NPC GameObjects
- Methods: `GetNPC(string id)` -> Find NPC by ID (e.g., `"igor_romanovich"`)
- Methods: `GetOrderedDistanceWarpPoints(Vector3 origin)` — warp points sorted by distance
- Methods: `GetNPCsInRegion(string regionId)` — NPCs in specific region
- **Key insight**: `FindObjectsOfType<NPC>()` misses NPCs inside buildings (inactive GOs). Always prefer `NPCManager.NPCRegistry` for complete NPC enumeration.

### NPCAnimation (`Il2CppScheduleOne.NPCs`) extends AvatarAnimation
- Namespace: `ScheduleOne.NPCs`
- Base class: `AvatarAnimation` (`Il2CppScheduleOne.AvatarFramework.Animation`)
- Methods: `LateUpdate()` - Virtual, overrides base. Sets Animator params every frame (Direction, Strafe)
- Methods: `UpdateMovementAnimation()` - Virtual, called from LateUpdate. Feeds walk/strafe to Animator
- Methods: `Awake()` - Caches NPC reference, WalkMapCurve
- Methods: `SetRagdollActive(bool)` - Ragdoll toggle
- **CRITICAL**: `LateUpdate` runs AFTER `Update` -> overwrites any Animator values set in `Update`
- **Fix**: `npcAnim.enabled = false` stops LateUpdate without killing the Animator itself
- **Key insight**: Animator component is SEPARATE - disabling NPCAnimation does NOT disable the Animator

### NPCScheduleManager (`Il2CppScheduleOne.NPCs`)
- Methods: `DisableSchedule()` - Stops schedule, prevents NPC from being warped home or set invisible
- Methods: `EnableSchedule()` - Re-enables schedule processing
- Found via `npc.gameObject.GetComponentInChildren<NPCScheduleManager>(true)` (include inactive)
- Same API pattern as S1API uses for NPC control

### NPCActions (`Il2CppScheduleOne.NPCs.Actions`)
- Accessed via `npc.Actions`
- Methods: `SetCanUseUmbrella(bool)` — Per-NPC umbrella control. Does NOT affect global weather system.
- **Gotcha**: `NPC.IsUnderCover` does NOT control umbrella logic — only weather entity status. Use `NPCActions.SetCanUseUmbrella(false)` instead.
- **Proven pattern** (IllegalRave): Call on warp + every frame in animation update, reverse in Cleanup per `SetCanUseUmbrella(true)`.

### NPCRelationData (`Il2CppScheduleOne.NPCs.Relation`)
- Accessed via `npc.RelationData`
- Methods: `Unlock(EUnlockType type, bool notify)` — Unlocks NPC for player interaction
  - `EUnlockType.Recommendation` — unlock via recommendation
  - `notify = false` — silent unlock, no UI popup, creates MSGConversation
- Methods: `ChangeRelationship(float amount, bool replicate)` — changes relationship value. Positive = improve, negative = worsen
- **Proven pattern** (Drones mod): `npc.RelationData.ChangeRelationship(repBonus, true)` after successful delivery to improve customer relationship
- **Proven pattern** (IllegalRave): Silent Igor unlock in button registration if NPC not yet unlocked. Conversation available on next retry tick.

### NPCAwareness (`Il2CppScheduleOne.NPCs`)
- NPC perception/detection system component
- Can be disabled to prevent red detection circles at NPC feet
- **Proven pattern** (IllegalRave): Disabled during rave, tracked in dictionary for re-enable on cleanup

### VisionCone (`Il2CppScheduleOne.Vision`)
- NPC vision cone component
- Disabled alongside NPCAwareness to remove visual indicators
- Tracked for re-enable on cleanup

### PoliceOfficer (`Il2CppScheduleOne.Police`)
- Extends `NPC`
- Filtered via `npc.TryCast<PoliceOfficer>()` — returns non-null for police NPCs
- **Proven pattern** (IllegalRave): Police NPCs excluded from rave participation (like Dealer exclusion)

### NPCHealth (`Il2CppScheduleOne.NPCs`)
- Properties: `Health` (float, get/set) — current HP (SyncVar)
- Properties: `MaxHealth` (float) — maximum HP
- Properties: `NormalizedHealth` (float) — Health/MaxHealth ratio
- Properties: `IsDead` (bool), `IsKnockedOut` (bool)
- Properties: `CanRevive` (bool), `ShouldSaveHealth` (bool)
- Properties: `Invincible` (bool, get/set) — invincibility toggle
- Methods: `TakeDamage(float amount, bool isLethal)` — deal damage
- Methods: `KnockOut()` — knock out NPC (KO state, not dead)
- Methods: `Revive()` — revive from KO/death
- Methods: `RestoreHealth()` — restore to full HP
- Methods: `Die()` — kill NPC
- Events: `onKnockedOut`, `onDie`, `onRevive`, `onTakeDamage`
- Static: `REVIVE_DAYS` (int) — days until auto-revive
- **Key insight**: KO ≠ Death. KO is recoverable state, Death requires Revive or REVIVE_DAYS

### NPCMovement (`Il2CppScheduleOne.NPCs`) — Extended
- Methods: `Warp(Vector3 position)` — main warp method (27 callers in game)
- Methods: `Warp(Transform target)` — warp to transform (position + rotation)
- Methods: `ReceiveWarp(Vector3 position)` — network RPC for warp sync
- Methods: `WarpToNavMesh()` — snap NPC back to NavMesh
- Methods: `PauseMovement()` / `ResumeMovement()` — pause/resume movement
- Methods: `SetAgentEnabled(bool)` — enable/disable NavMeshAgent
- Methods: `Stop()` — stop movement
- Methods: `SetDestination(Vector3 position)` — set movement target
- Methods: `ActivateRagdoll_Server()` — server-side ragdoll activation
- Methods: `ActivateRagdoll(Vector3 forcePoint, Vector3 forceDir, float forceMagnitude)` — ragdoll with force
- Methods: `DeactivateRagdoll()` — disable ragdoll
- Properties: `Agent` — NavMeshAgent reference
- Properties: `RagdollDraggable` — Draggable component
- Events: `onRagdollStart`, `onRagdollEnd` (UnityEvent)
- Static: `RAGDOLL_THRESHOLD`, `MOMENTUM_RAGDOLL_THRESHOLD` (float)
- **Proven NPC warp pattern** (from IllegalRave mod):
  1. `npc.Movement.PauseMovement()`
  2. `npc.Movement.SetAgentEnabled(false)`
  3. `NPCScheduleManager.DisableSchedule()` + `npc.Behaviour.enabled = false`
  4. `npc.transform.position = targetPos`
  5. `npc.SetVisible(true, false)`
  6. Cleanup reverse: `Behaviour.enabled = true` → `EnableSchedule()` → `SetAgentEnabled(true)` → `ResumeMovement()`

### NPC Selection Pattern (Proven — from IllegalRave)
```csharp
Il2CppSystem.Collections.Generic.List<NPC> allNPCs = NPCManager.NPCRegistry;
var available = new List<NPC>();
for (int i = 0; i < allNPCs.Count; i++)
{
    NPC npc = allNPCs[i];
    if (npc == null) continue;
    try { if (npc.TryCast<Dealer>() != null) continue; } catch { }
    try { if (npc.TryCast<PoliceOfficer>() != null) continue; } catch { }
    try { if (npc.IsCustomNPC) continue; } catch { }  // exclude S1API NPCs
    if (!npc.IsConscious || npc.IsInVehicle) continue;
    available.Add(npc);
}
```

### Animator Parameters (NPC Locomotion) - Verified at Runtime

#### Movement (Floats)
| Parameter | Type | Range | Effect |
|-----------|------|-------|--------|
| `Direction` | Float | 0.0-1.0 | 0=idle, >0=walk/run blend tree |
| `Strafe` | Float | -1.0 to 1.0 | Side movement blend |
| `TimeAirborne` | Float | 0.0+ | Time since leaving ground |
| `ClimbSpeed` | Float | 0.0+ | Climbing animation speed |

#### State (Bools)
| Parameter | Type | Effect |
|-----------|------|--------|
| `isGrounded` | Bool | **CRITICAL** - Must be True for locomotion. False = Airborne (Direction/Strafe ignored) |
| `isCrouched` | Bool | Crouch pose |
| `Sitting` | Bool | Sitting pose |
| `Smoking` | Bool | Smoking animation |
| `HandsUp` | Bool | Hands up pose |
| `SkateIdle` | Bool | Skateboard idle |
| `Drinking` | Bool | Drinking animation |
| `IsClimbing` | Bool | Climbing state |
| `PatDown` | Bool | Pat-down animation |
| `UsePackagingStation` | Bool | Work animation |
| `PourItem` | Bool | Pouring animation |
| `UseChemistryStation` | Bool | Chemistry station |
| `UseHammer` | Bool | Hammer animation |
| `UseSprayCan` | Bool | Spray can animation |

#### Triggers
| Parameter | Effect |
|-----------|--------|
| `Jump` | Jump animation |
| `Punch` | Punch animation |
| `ThumbsUp` | Thumbs up gesture |
| `DisagreeWave` | Disagree wave gesture |
| `Nod` | Nod gesture |
| `ConversationGesture1` | Conversation gesture |
| `GrabItem` | Grab item animation |
| `StandUp_Front` / `StandUp_Back` | Stand up from ragdoll |
| `Flinch_*` | Flinch reactions (8 variants: direction + heavy) |
| `SkatePush` | Skateboard push |
| `Eat` | Eating animation |
| `Snort` | Snorting animation |

- **Gotcha**: Remote NPCs (LOD/culled) may not have Animator loaded. Use lazy-init (retry each frame)
- **Gotcha**: Il2Cpp cached Animator refs become stale after avatar reload. Check `WasCollected` before use
- **Gotcha**: `SetVisible(true, false)` can re-enable `NPCAnimation`. Re-disable every frame
- **Gotcha**: Animator param names are case-sensitive. `isCrouched` works, `Crouched` does not

#### Every-Frame Ground Enforcement (Confirmed Pattern)
`TrySetMovementAnimation()` must reset ALL blocking bools every frame:
- `isGrounded = True`, `TimeAirborne = 0` (prevents Airborne state)
- `Sitting`, `Smoking`, `HandsUp`, `SkateIdle`, `Drinking`, `IsClimbing` = False
- Exception: `isCrouched` NOT reset here (CrouchBounce controls it explicitly)
- Without this reset, NPCs in Sitting/Smoking states remain stuck forever

### AvatarEmotionManager (`Il2CppScheduleOne.AvatarFramework.Emotions`)

### Avatar (`Il2CppScheduleOne.AvatarFramework`) extends MonoBehaviour
- Methods: `GetMugshot(Action<Texture2D> callback)` — **proactive mugshot generation**. Renders NPC face via MugshotGenerator camera and returns Texture2D in callback. Works without phone map being open. Use to pre-populate `NPC.MugshotSprite`.
- Methods: `LoadAvatarSettings(AvatarSettings)`, `ApplyBodySettings(AvatarSettings)`, `ApplyHairSettings(AvatarSettings)`, etc.
- Properties: `CurrentSettings` -> `AvatarSettings` (get/set)
- Properties: `InitialAvatarSettings` -> `AvatarSettings` (field)
- **Key insight**: The game's mugshot system is lazy — `NPC.MugshotSprite` stays null until the phone map opens (triggers MugshotGenerator). Call `Avatar.GetMugshot()` proactively to force generation at mod init time. Callback should create Sprite via `Sprite.Create(tex, ...)` and set `npc.MugshotSprite`.

### MugshotGenerator (`Il2CppScheduleOne.AvatarFramework`) extends MonoBehaviour
- Methods: `GenerateMugshot()` — renders using DefaultSettings
- Methods: `GenerateMugshot(AvatarSettings, bool, Action<Texture2D>)` — renders specific avatar
- Methods: `FinalizeMugshot()` — called internally after render
- Fields: `MugshotRig`, `DefaultSettings`, `LookAtPosition`, `finalTexture`, `generate`
- **Note**: Prefer `Avatar.GetMugshot()` over direct MugshotGenerator access — Avatar handles rig setup automatically.

### AvatarEmotionManager (`Il2CppScheduleOne.AvatarFramework.Emotions`)
- Methods: `AddEmotionOverride(string emotionName, string label, float duration, int priority)` - Duration=0 = permanent, Priority=10 = high
- Methods: `RemoveEmotionOverride(string label)` - Remove by label string
- Emotion names (`DefaultEmotions` static class, game v0.44+): `Happy`, `Cheery`, `Surprised`, `Angry`, `Concerned`, `Annoyed`, `Scared`, `Shroom`, `Cocaine`, `Zombie`, `Meth`
- Removed in 0.44: `Sad`, `Confused`
- Also exists: `Neutral`, `Sleeping` (special states, not standard emotions)

### PunchController (`Il2CppScheduleOne.Combat`)
- Player-side melee controller
- Properties: `PunchingEnabled` (bool, get/set) — punching active?
- Properties: `IsLoading` (bool) — charging punch?
- Properties: `IsPunching` (bool) — executing punch?
- Properties: `player` — Player reference
- Properties: `punchLoad` (float) — current charge level
- Properties: `remainingCooldown` (float) — remaining cooldown
- Properties: `PunchSound` (AudioSourceController), `PunchAnimator` (RuntimeAnimatorController)
- Methods: `CanStartLoading()` (bool), `StartLoad()`, `Release()` — charge cycle
- Methods: `Punch(float power)` — execute punch with power
- Methods: `ExecuteHit(float power)` — register hit
- Methods: `SetPunchingEnabled(bool)` — toggle punching
- Static: `MAX_PUNCH_LOAD`, `MIN_COOLDOWN`, `MAX_COOLDOWN`, `PUNCH_RANGE`, `PUNCH_DEBOUNCE` (float)

### CombatBehaviour (`Il2CppScheduleOne.Combat`) extends Behaviour
- NPC combat AI with Activate/Deactivate/Pause/Resume/Disable/BehaviourUpdate lifecycle
- **Target management:**
- Methods: `SetTargetAndEnable_Server(NetworkObject)` — set target + activate (server)
- Methods: `SetTarget(NetworkObject)` — virtual, set combat target
- Methods: `StartCombat()` / `EndCombat()` — virtual, combat lifecycle
- **Combat logic:**
- Methods: `ReadyToAttack(bool checkTarget)` — virtual, ready check
- Methods: `Attack()` — virtual, execute attack
- Methods: `SucessfulHit()` — hit callback (note: typo in game source)
- Methods: `TargetSpotted()` — virtual, target discovered
- Methods: `IsTargetValid()` — virtual, target still valid?
- Methods: `IsTargetVisibleThisFrame()`, `IsTargetInRange(Vector3)` — visibility/range checks
- Methods: `GetMinTargetDistance()`, `GetMaxTargetDistance()` — combat range
- Methods: `MarkPlayerVisible()` — mark player as visible
- **Weapons:**
- Methods: `SetWeapon(string weaponPath)` — virtual, equip weapon
- Methods: `ClearWeapon()` — RPC, remove weapon
- Properties: `currentWeapon` (AvatarWeapon)
- **Fields:**
- `_Target_k__BackingField` (NetworkObject) — current target
- `GiveUpRange` (float), `GiveUpAfterSuccessfulHits` (int) — give-up conditions
- `DefaultMovementSpeed` (float), `DefaultSearchTime` (float)
- `DefaultWeapon` (AvatarWeapon), `CombatOnStart` (bool)
- `successfulHits` (int), `consecutiveMissedShots` (int)
- **Access path**: `npc.Behaviour.CombatBehaviour` — direct field on NPCBehaviour

### Impact (`Il2CppScheduleOne.Combat`)
- Hit data structure
- Properties: `HitPoint` (Vector3), `ImpactForceDirection` (Vector3)
- Properties: `ImpactForce` (float), `ImpactDamage` (float)
- Properties: `ImpactType` (EImpactType), `ImpactSource` (NetworkObject), `ImpactID` (int)
- Methods: `IsLethal(EImpactType)` — static, is type lethal?
- Methods: `IsPlayerImpact(out Player)` — was it player hit?

### EImpactType (`Il2CppScheduleOne.Combat`)
- Enum: `Punch`, `BluntMetal`, `SharpMetal`, `Bullet`, `PhysicsProp`, `Explosion`

### IDamageable (`Il2CppScheduleOne.Combat`)
- Interface: `gameObject`, `SendImpact(Impact)`, `ReceiveImpact(Impact)`
- Implemented by NPC and other damageable objects

### TimeManager (`Il2CppScheduleOne.GameTime`)
- Properties: `CurrentTime` -> int (HHMM format, e.g., 2300 = 23:00)
- Properties: `ElapsedDays` -> int (increments at midnight, NOT only on sleep)
- Properties: `IsSleepInProgress` -> bool
- **Key insight**: Time freezes at 0400 until player sleeps. Game clock goes 0700->2359->0000->0400.
- **Key insight**: ElapsedDays increments at midnight. Day-dependent flags must reset on day change.
- **Key insight**: Time checks must not nest evening (>=2300) and post-midnight (<500) - they are independent conditions
- **Key insight**: Sleeping triggers a scene reload (non-Main scene → Main scene). MelonLoader `OnSceneWasLoaded` fires twice: first with a loading/transition scene, then with "Main". Any mod state stored in static fields that is reset in a scene-change handler will be lost after every sleep. Session-lifetime flags (e.g., one-time messages) must NOT be cleared on scene change — only on game process restart (static field re-initialization). Discovered via BUG-013 (Illegal Rave): DJ pitch SMS was re-sent after every sleep because `Reset()` cleared a session flag on scene change.

### DialogueController (`Il2CppScheduleOne.Dialogue`)
- Base class for NPC dialogues, sits as Component on NPC GameObject
- **Lifecycle:**
- Methods: `Start()` (virtual), `Hovered()`, `Interacted()` — game callbacks
- Methods: `StartGenericDialogue(bool allowExit)`, `CanStartDialogue()` (virtual)
- Methods: `SetDialogueEnabled(bool)` — enable/disable dialogue
- **Choices (dynamic):**
- Methods: `AddDialogueChoice(DialogueChoice choice, int priority)` — virtual, returns index
- Methods: `GetActiveChoices()` — List\<DialogueChoice\>
- Methods: `ChoiceCallback(string choiceLabel)` — virtual, called when choice selected (label identifies)
- Methods: `CheckChoice(string choiceLabel, out string invalidReason)` — virtual, validate choice
- Methods: `ModifyChoiceList(string dialogueLabel, ref List<DialogueChoiceData>)` — virtual
- Methods: `ModifyChoiceText(string choiceLabel, string choiceText)` — virtual
- **Greetings:**
- Methods: `GetActiveGreeting(out bool playVO, out EVOLineType)`, `GetCustomGreeting(...)` (virtual)
- Methods: `AddGreetingOverride(GreetingOverride)` — virtual
- **Container/Branching:**
- Methods: `SetOverrideContainer(DialogueContainer)`, `ClearOverrideContainer()`
- Methods: `DecideBranch(string branchLabel, out int index)` — virtual
- Methods: `ModifyDialogueText(string label, string text)` — virtual
- **Fields:**
- `IntObj` (InteractableObject), `GenericDialogue` (DialogueContainer)
- `DialogueEnabled` (bool), `npc` (NPC), `handler` (DialogueHandler)
- `Choices` (List\<DialogueChoice\>), `GreetingOverrides` (List\<GreetingOverride\>)
- Static: `GREETING_COOLDOWN` (float)
- **Key insight**: Use `AddDialogueChoice()` to inject custom choices into any NPC's dialogue. React in `ChoiceCallback(string label)`. See `DialogueController_Fixer` for subclass example (overrides ChoiceCallback, ModifyChoiceList, CheckChoice, DecideBranch)

### DialogueController.DialogueChoice (Nested Class)
- Properties: `Enabled` (bool), `ChoiceText` (string), `Priority` (int)
- Properties: `ShowWorldspaceDialogue` (bool), `Conversation` (DialogueContainer)
- Properties: `onChoosen` (UnityEvent) — callback when chosen
- Delegates: `shouldShowCheck` (ShouldShowCheck), `isValidCheck` (IsChoiceValid — out invalidReason)

### DialogueController.GreetingOverride (Nested Class)
- Properties: `Greeting` (string), `ShouldShow` (bool), `PlayVO` (bool), `VOType` (EVOLineType)

### DialogueHandler (`Il2CppScheduleOne.Dialogue`)
- Processes dialogue flow (nodes, branches, choices)
- Methods: `InitializeDialogue(DialogueContainer)`, `InitializeDialogue(string name, bool enableBehaviour, string entryLabel)`
- Methods: `ShowNode(DialogueNodeData)`, `EvaluateBranch(BranchNodeData)`, `ChoiceSelected(int index)`
- Methods: `ContinueSubmitted()`, `OverrideShownDialogue(string text)`, `StopOverride()`
- Methods: `SkipNextDialogueBehaviourEnd()`, `CreateTempLink(string baseNode, string baseOption, string targetNode)`

### InteractableObject (`Il2CppScheduleOne.Interaction`)
- Methods: `SetInteractionType(EInteractionType)`, `SetInteractableState(EInteractableState)`, `SetMessage(string)`
- Methods: `CheckAngleLimit(Vector3)` — angle check for interaction
- Properties: `message` (string), `MaxInteractionRange` (float), `RequiresUniqueClick` (bool)
- Properties: `displayLocationPoint` (Transform), `LimitInteractionAngle` (bool), `AngleLimit` (float)
- Events: `onHovered`, `onInteractStart`, `onInteractEnd` (UnityEvent)

## Messaging System

### MessagingManager (`Il2CppScheduleOne.Messaging`)
- Found via `FindObjectOfType<MessagingManager>()`
- Methods: `GetConversation(NPC npc)` -> `MSGConversation`

### MSGConversation (`Il2CppScheduleOne.Messaging`)
- Methods: `SendMessage(Message message, bool arg1, bool arg2)` - send a message in conversation
- Properties: `Sendables` -> list of `SendableMessage` (response buttons for player)

### Message (`Il2CppScheduleOne.Messaging`)
- Constructor: `new Message(string text, Message.ESenderType senderType, bool arg1, int arg2)`
- `ESenderType.Player` = 0, `ESenderType.Other` = 1 (incoming message appearance)
- **Confirmed working** at runtime for SMS from NPCs

### SendableMessage (`Il2CppScheduleOne.Messaging`)
- Constructor: `new SendableMessage(string text, MSGConversation conversation)`
- Properties: `Text` (string), `onSent` (Action callback), `disableDefaultSendBehaviour` (bool)
- Add to `conversation.Sendables` to show as player response button
- Set `disableDefaultSendBehaviour = true` to prevent actual send (use for custom actions)

### ClassInjector (`Il2CppInterop.Runtime.Injection`)
- Methods: `RegisterTypeInIl2Cpp<T>()` — registers managed types as Il2Cpp types
- Must be called in `OnLateInitializeMelon` (after Il2Cpp domain is ready)
- Required for: custom `ItemFilter` subclasses, custom `MonoBehaviour` subclasses

### Il2Cpp Proxy Object Behavior
- Il2Cpp proxy objects can have valid managed references but destroyed native backing
- `obj != null` passes in managed code, but accessing properties (e.g., `slot.SlotIndex`) throws native NullRef
- Native Il2Cpp NullRefs are wrapped as `Il2CppException`, NOT `System.NullReferenceException`
- Finalizer/catch must check: `exception is Il2CppException && message.Contains("NullReferenceException")`
- For save paths: defensive try/catch with fallback values is the safest pattern

### Il2Cpp Cast Patterns
- `obj.Cast<T>()` — hard cast, throws `InvalidCastException` if runtime type doesn't match
- `obj.TryCast<T>()` — safe cast, returns null if type doesn't match
- **Rule**: Use `TryCast` for any item/slot content. Use `Cast` only for known interface implementations (e.g., `IGUIDRegisterable`)

### ItemDefinition (`Il2CppScheduleOne.ItemFramework`)
- Base class for all item definitions (static data — NOT instances)
- Properties: `ID` (string), `Name` (string), `Icon` (Sprite), `Category` (EItemCategory)
- Can be subclassed for custom item types (e.g., `DroneStationDefintion`)
- **`Icon`**: Sprite used in game UI. Access with try/catch in Il2Cpp (proxy may be invalid)
- **Key insight**: Icons are per-definition, not per-instance. Cache by `ItemInstance.ID` → `definition.Icon`
- Inheritance: `BaseItemDefinition` → `ItemDefinition` → `StorableItemDefinition` → `QualityItemDefinition` → `PropertyItemDefinition` → `ProductDefinition`

### EItemCategory (`Il2CppScheduleOne.Core.Items.Framework`)
- Enum in `Il2CppScheduleOne.Core.dll` (moved from `Il2CppScheduleOne.ItemFramework` in 0.44)
- Values include Product, Storage, and others (integer values)
- `(EItemCategory)4` used by DroneStationDefintion
- Used by `FilterConfigPanel.SearchCategory.Category` to group items in filter grid

### StorableItemDefinition (`Il2CppScheduleOne.ItemFramework`) extends ItemDefinition
- For items that can be stored in ItemSlots
- `PackagingDefinition` extends this

### ProductDefinition (`Il2CppScheduleOne.Product`) extends PropertyItemDefinition
- Inherits `Category`, `ID`, `Icon` from ItemDefinition
- Castable to `ItemDefinition` via `product.TryCast<ItemDefinition>()`
- Discovered products tracked in `ProductManager.DiscoveredProducts`

### ProductManager (`Il2CppScheduleOne.Product`)
- Static field: `DiscoveredProducts` (List\<ProductDefinition\>) — all products the player has discovered
- Instance field: `AllProducts` (List\<ProductDefinition\>) — all product definitions in the game
- Instance fields: `WeedMixMap`, `MethMixMap`, `CokeMixMap`, `ShroomMixMap` (MixerMap)
- Private instance field: `createdProducts` (List\<ProductDefinition\>) — runtime-created mixes/products
- Methods: `DiscoverProduct(string productID)`, `SetProductDiscovered(NetworkConnection, string, bool)`
- Methods: `GetKnownProduct(EDrugType, List<Effect>)` — resolves known mixed products by effect set
- Static bools: `MethDiscovered`, `CocaineDiscovered`, `ShroomsDiscovered`
- **Key insight**: `DiscoveredProducts` is static — accessible without singleton lookup
- **Key insight**: Some valid mix products can exist in `AllProducts`/`createdProducts` while missing from `DiscoveredProducts`; filter population should merge both sources when compatibility issues appear

### Price Fields (what lives where)
- `ProductDefinition.Price` (float) — internal base price, visible in source.
- `ProductDefinition.MarketValue` (float) — market value for customer pricing calculations.
- `ProductDefinition.BasePrice` (float) — base for mix calculation (strain raw value).
- `ProductManager.ProductPrices` (Dictionary&lt;ProductDefinition, float&gt;) — **live sale price** per product in the running game. Serialized to `Products.json` save as `{String, Int}` pairs.
- `StorableItemDefinition.BasePurchasePrice` (float) — purchase price for buyable items (Stations, Furniture). Read via cast: `def.TryCast<StorableItemDefinition>()?.BasePurchasePrice`.
- `StorableItemDefinition.ResellMultiplier` (float) — resell factor when selling back to shops.

**Anti-Pattern**: Hardcoding prices statically in C#. They change with game version and with player pricing decisions (`ProductManager.ProductPrices` is mutated at runtime). Always prefer runtime lookup.

### Vanilla Drug Defaults (from `StreamingAssets/DefaultSave/Products.json`)
These six IDs are confirmed in the vanilla game and carry default sale prices. Other `EDrugType` enum values (MDMA, Shrooms, Heroin) have no confirmed stable item ID — resolver must check `Registry.GetItem(id) != null`.

| ID | Display | EDrugType | Default Price |
|----|---------|-----------|---------------|
| `ogkush` | OG Kush | Marijuana | $38 |
| `sourdiesel` | Sour Diesel | Marijuana | $40 |
| `greencrack` | Green Crack | Marijuana | $43 |
| `granddaddypurple` | Granddaddy Purple | Marijuana | $44 |
| `meth` | Methamphetamine | Methamphetamine | $70 |
| `cocaine` | Cocaine | Cocaine | $150 |

### EDrugType (`Il2CppScheduleOne.Product`)
Complete enum: `Marijuana`, `Methamphetamine`, `Cocaine`, `MDMA`, `Shrooms`, `Heroin`. Used in `ProductDefinition.DrugType` and `Initialize(List<Effect>, List<EDrugType>)`. All values are Contraband by definition (DrugTrafficking crime path in `Law.json`).

### Legality
- **No single `IsIllegal` property** on `ItemDefinition`. Legality is modelled through Crime-Detection and `ItemFilter_LegalStatus`, not through a flag on the item.
- `ItemFilter_LegalStatus.RequiredLegalStatus` (Filter-Field) — for UI/slot filtering. The enum itself (Legal/Restricted/Illegal-equivalent) is referenced in the filter class.
- Drugs are treated as illegal via `EDrugType` and cop AI (DrugTrafficking crime).
- Firearms trigger cop reaction on sight (they are contraband in the game context).
- Heuristic for your own code: druggy = `def.TryCast<ProductDefinition>() != null` or `EDrugType` match.

### Registry Enumeration
- `Registry.Instance.GetAllItems()` → `Il2CppSystem.Collections.Generic.List<ItemDefinition>`. **Important**: Return type is `Il2CppSystem.Collections.Generic.List`, NOT `System.Collections.Generic.List` — implicit cast fails (CS0029). Declare variable explicitly.
- `Registry.GetItem(string id)` → `ItemDefinition` or null. Case-insensitive in vanilla.
- `Registry._GetItem(string id, bool warnIfNonExistent = true)` — internal path, warns on miss.
- `Registry.ItemExists(string id)` — bool probe without warning.
- `Registry.ItemDictionary` — Dictionary&lt;string, ItemDefinition&gt; for direct access.

### Weapon IDs (confirmed from `WeaponSpawner.RevolverIdCandidates`)
Vanilla IDs for firearms resolvable via `Registry.GetItem`:
- `revolver` — primary Contratto spawn candidate
- `m1911` — fallback
- `pistol` — generic fallback

Other weapon candidates (community-known, NOT all confirmed in current build): `baseballbat`, `machete`. Always check existence via `ExistsInRegistry` before spawning.

### Expensive Stations (community-known IDs, prices via runtime lookup)
IDs from vanilla asset bundle. **Do NOT enter prices statically here** — read `StorableItemDefinition.BasePurchasePrice`.
- `brickpress` — Mid-tier processing
- `chemistrystation` — Meth/Cocaine synthesis
- `cauldron` — Cocaine pipeline
- `labovenmk2`, `mixingstationmk2`, `packagingstationmk2` — Premium upgrades

### Runtime Resolver Pattern (in `ItemCatalog.GetRuntimePrice`)
```csharp
var def = Registry.GetItem(id);
if (def == null) return 0;

// ProductDefinition: ProductManager has the live price.
var prod = def.TryCast<ProductDefinition>();
if (prod != null)
{
    var pm = ProductManager.Instance;
    if (pm?.ProductPrices != null && pm.ProductPrices.ContainsKey(prod))
        return (int)pm.ProductPrices[prod];   // float → int cast REQUIRED (CS0266)
    return (int)prod.MarketValue;             // Fallback
}

// StorableItemDefinition: BasePurchasePrice
var storable = def.TryCast<StorableItemDefinition>();
if (storable != null) return (int)storable.BasePurchasePrice;
```

**Gotcha**: `ProductPrices[prod]` is `float`, not `int` — implicit cast fails (CS0266). Explicitly `(int)`-cast.

## UI / Filter System

### FilterConfigPanel (`Il2CppScheduleOne.UI.Items`)
- MonoBehaviour: manages the slot filter configuration UI (whitelist/blacklist + item picker + quality selector)
- Properties: `OpenSlot` (ItemSlot, get/set), `IsOpen` (bool, get/set)
- Fields: `SearchItemPrefab` (GameObject) — prefab for search grid items
- Fields: `ItemEntryPrefab` (GameObject) — prefab for allowed-items list entries
- Fields: `searchCategories` (List\<SearchCategory\>) — populated during `Open()`
- Fields: `CategoryContainer` (RectTransform), `SearchContainer` (RectTransform)
- Fields: `SearchInput` (TMP_InputField), `ApplyToSiblingsButton` (Button)
- Methods: `Open(ItemSlotUI ui)` — opens the filter panel for a slot, populates searchCategories
- Methods: `Close()`, `UpdateSearch()`, `RefreshSearchResults()`, `OpenSearch()`, `CloseSearch()`
- Methods: `GetSearchCategory(EItemCategory)` — finds or creates SearchCategory for given category
- Methods: `ItemClicked(string itemID)` — called when search grid item clicked (private, accessible via Il2Cpp proxy)
- Methods: `AddItem(string itemID)` — adds item to filter (private, accessible via Il2Cpp proxy)
- Methods: `RemoveItem(string itemID)` — removes item from filter
- Methods: `FilterModeSelected(SlotFilter.EType)`, `QualitySelected(EQuality)`, `ApplyToSiblingsClicked()`
- Methods: `RefreshDisplay()` — refreshes the allowed-items display
- **Key insight**: `Open()` populates `searchCategories` synchronously, then may start animation coroutine. Harmony Postfix on `Open` can safely access `searchCategories`
- **Key insight (CHANGE-098)**: `UpdateSearch()` + `RefreshSearchResults()` handle visibility/search filtering but do NOT fully initialize newly instantiated entries. For injected entries, icon (`Image.sprite`), tooltip (`Tooltip.text`), TMP text (`TMP_Text.text`), and click handler (`Button.onClick`) must be set MANUALLY before or after `AddItem()`.
- **Key insight (BUG-043)**: Vanilla `Open()` reads `<SlotOwner>k__BackingField` directly in native Il2Cpp code (field access, not property getter). Harmony postfix on `get_SlotOwner` does NOT intercept this. For drone station slots without native SlotOwner, the vanilla pipeline misses products. Robust fix: inject missing products from both `ProductManager.DiscoveredProducts` and `ProductManager.Instance.AllProducts` in Postfix, then rebuild via `UpdateSearch()` + `RefreshSearchResults()`.
- **Key insight (CHANGE-098)**: Search entry prefab instances carry a `Tooltip` component (`Il2CppScheduleOne.UI.Tooltips.Tooltip`) with a `text` field that controls the hover label in the search grid. When cloning existing entries, this field must be overwritten — it is NOT updated by `UpdateSearch()`/`RefreshSearchResults()`.
- **Key insight (CHANGE-098)**: Best injection approach: clone an existing initialized entry (preserves layout, component wiring), then overwrite `Tooltip.text`, `TMP_Text.text`, `Image.sprite`, and `Button.onClick`. Using raw `SearchItemPrefab` produces blank tiles because it requires full vanilla initialization that is only performed inside native `Open()`.

### Tooltip (`Il2CppScheduleOne.UI.Tooltips`)
- MonoBehaviour on UI elements providing hover labels
- Fields: `text` (string, offset 0x20) — hover label text, public
- Fields: `labelOffset` (Vector2), `LabelOriginRect` (RectTransform)
- Properties: `isWorldspace` (bool, private set), `labelPosition` (Vector3, get)
- **Key insight (CHANGE-098)**: Search grid entries have `Tooltip` components. When cloning entries, `Tooltip.text` retains the source entry's name and MUST be overwritten for injected products.

### FilterConfigPanel.SearchCategory (Nested Class)
- Fields: `Category` (EItemCategory), `Container` (RectTransform), `Items` (List\<Item\>)
- Methods: `AddItem(ItemDefinition item, RectTransform entry)` — registers item with its UI entry
- Methods: `GetItem(string itemID)` — returns Item or null
- Methods: `SetSearch(string search)` — filters items by search text

### FilterConfigPanel.SearchCategory.Item (Nested Class)
- Fields: `ItemDefinition` (ItemDefinition), `Entry` (RectTransform)

### ItemSlot Backing Field Behavior (`Il2CppScheduleOne.ItemFramework`)
- `<SlotOwner>k__BackingField` — auto-property backing field for `SlotOwner`
- `<SiblingSet>k__BackingField` — auto-property backing field for `SiblingSet`
- **CRITICAL**: Native Il2Cpp code reads backing fields DIRECTLY, bypassing managed property getters. Harmony postfixes on `get_SlotOwner` or `get_SiblingSet` are NOT called by native callers
- **CRITICAL**: Writing to `<SlotOwner>k__BackingField` via `Marshal.WriteIntPtr` causes ACCESS_VIOLATION for interface-typed fields (`IItemSlotOwner`). Il2Cpp stores interface pointers differently from class pointers
- **Proven pattern**: Use Harmony postfix on property getter for managed callers + separate injection logic (e.g., `InjectMissingProducts`) for native callers that bypass the getter

### SlotFilter (`Il2CppScheduleOne.ItemFramework`)
- Properties: `Type` (EType enum: None, Whitelist, Blacklist), `AllowedItemIDs` (List\<string\>), `AllowedQualities` (List\<EQuality\>)
- Methods: `Clone()` — creates independent copy (important: vanilla ServerRpc implicitly clones via serialization, bypass must do it manually)
- Methods: `DoesItemMatchFilter(ItemInstance)` — checks item against filter (returns true if item passes the Whitelist/Blacklist)
- Used by `ItemSlot.PlayerFilter` for player-configured slot filters

## Camera / Rendering System

### PlayerCamera (`Il2CppScheduleOne.PlayerScripts`)
- Component on `Camera.main` GameObject
- Repositions `Camera.main` to player position every frame
- Disabling the component stops Camera.main from tracking the player
- **Key insight**: To move Camera.main elsewhere (e.g., drone position), disable `PlayerCamera` component FIRST, then reposition. Re-enable on exit
- **Key insight**: `Camera.main` is the ONLY camera that game distance-based systems use. Freecam works because it IS Camera.main, not because it's a separate camera

### AvatarImpostor (`Il2CppScheduleOne.NPCs`)
- NPC billboard system — renders 2D billboard quad instead of 3D model at distance
- Methods: `EnableImpostor()` — activates billboard quad
- Methods: `DisableImpostor()` — deactivates billboard, shows 3D model via LODGroup
- Lifecycle: `LateUpdate()` — checks Camera.main distance, toggles billboard on/off per frame
- Fields: `meshRenderer` (MeshRenderer) — the billboard quad renderer
- Fields: `cachedCamera` — internal field, NOT used for distance checks in native LateUpdate (despite name)
- **Patching strategy**: Patch `LateUpdate()` or `EnableImpostor()` (lifecycle entry points). Do NOT patch `DisableImpostor()` — il2Cpp native-to-native calls in LateUpdate bypass managed Harmony proxies for internal helpers
- **Key insight**: `meshRenderer.enabled = false` hides billboard without destructive side effects. Calling `DisableImpostor()` per-frame causes Employee.OnDestroy NullRefs + performance stutter
- **Key insight**: When Camera.main is at drone position (CHANGE-023), LODGroup automatically shows 3D models for nearby NPCs. Blocking `EnableImpostor()` prevents LateUpdate from switching back to billboard

### UnluckDistanceDisabler (`Il2CppScheduleOne.ObjectScripts` — inferred)
- Distance-based object streaming system
- Disables/enables game objects based on distance to `Camera.main` position
- **Key insight**: Uses Camera.main position EXCLUSIVELY — not any per-camera check. Mod cameras at other positions see "empty world" if Camera.main is at player position

### LODGroup (Unity Engine)
- Standard Unity LOD system, used by Schedule I for NPC model quality tiers
- LOD level selection based on Camera.main position
- **Key insight**: When Camera.main is repositioned to drone, LODGroup automatically selects high-detail models for NPCs near drone. No manual LOD override needed

## Unity Rendering Patterns (Game-Relevant)

### RawImage.color (LDR Limitation)
- UI color multiplier on `UnityEngine.UI.RawImage`
- **CRITICAL**: Values >1.0 have NO visible effect on standard RenderTextures (LDR clamps at 1.0)
- Can only darken/tint, NEVER brighten
- **Correct pattern for visual effects over LDR feeds**: Use a separate overlay Image layer with inverse alpha control (CHANGE-032)

### RenderTexture Lifecycle
- Create with `new RenderTexture(width, height, depth)` — use `Screen.width × Screen.height` for native resolution
- Assign to camera `targetTexture` BEFORE enabling camera
- **MUST call `Release()`** when done — otherwise GPU memory leak
- Default 1024×1024 sufficient for phone UI thumbnails, screen-resolution needed for fullscreen

### Camera Patterns
- `Camera.main.cullingMask`: Copy to mod cameras to match game's layer setup. Using `-1` (all layers) renders mod artifacts on excluded layers
- `Camera.useOcclusionCulling = false`: Required for mod cameras — they have no baked occlusion context
- `camera.enabled = true` + `targetTexture`: Preferred over manual `Camera.Render()` calls — more reliable + consistent

### CasinoGameController (`Il2CppScheduleOne.Casino`) extends NetworkBehaviour
- Abstract base for all casino games
- Properties: `IsOpen` (bool, get/set)
- Properties: `LocalPlayerData` (CasinoGamePlayerData), `Players` (CasinoGamePlayers)
- Properties: `Interaction` (CasinoGameInteraction)
- Properties: `DefaultCameraTransforms` (Transform[]), `localDefaultCameraTransform` (Transform)
- Methods: `OnLocalPlayerRequestJoin(Player)` — virtual, player join
- Methods: `Exit(ExitAction action)` — virtual, exit game
- Methods: `Open()` / `Close()` — virtual, game state
- Methods: `FixedUpdate()` — virtual, per-frame tick
- Static: `FOV` (float), `CAMERA_LERP_TIME` (float) — camera constants

### CasinoGameInteraction (`Il2CppScheduleOne.Casino`)
- Properties: `GameName` (string), `Players` (CasinoGamePlayers), `IntObj` (InteractableObject)
- Properties: `onLocalPlayerRequestJoin` (Action\<Player\>) — join callback

### CasinoGamePlayers (`Il2CppScheduleOne.Casino`)
- Network-synchronized player management for casino games
- Methods: `AddPlayer(Player)`, `RemovePlayer(Player)`, `GetPlayer(int index)`
- Methods: `SetPlayerScore(Player, int)`, `GetPlayerScore(Player)` (int)
- Properties: `PlayerLimit` (int), `Players` (Player[])
- Properties: `playerScores` (Dictionary\<Player, int\>), `playerDatas` (Dictionary\<Player, CasinoGamePlayerData\>)
- Events: `onPlayerListChanged`, `onPlayerScoresChanged` (UnityEvent)

### CasinoGamePlayerData (`Il2CppScheduleOne.Casino`)
- Key-value data store per player in a casino game
- Properties: `Parent` (CasinoGamePlayers), `Player` (Player)
- Properties: `bools` (Dictionary\<string, bool\>), `floats` (Dictionary\<string, float\>)
- Methods: `GetData<T>(string key)`, `SetData<T>(string key, T value, bool network)`

### RTBGameController (`Il2CppScheduleOne.Casino`) extends CasinoGameController
- Red-The-Ball card-based betting game with stages
- Properties: `LocalPlayerBet` (float, get/set), `LocalPlayerBetMultiplier` (float, get/set)
- Properties: `MultipliedLocalPlayerBet` (float, get)
- Properties: `CurrentStage` (EStage, get/set), `IsQuestionActive` (bool), `RemainingAnswerTime` (float)
- Properties: `playersInCurrentRound` (List\<Player\>)
- Methods: `SetLocalPlayerBet(float)`, `GetNetBetMultiplier(EStage)` — static
- Methods: `SetLocalPlayerAnswer(float)`, `NotifyAnswer(float answerIndex)`
- Methods: `EndGame()`, `RemoveLocalPlayerFromGame(bool payout, float cameraDelay)`
- Methods: `IsCurrentRoundEmpty()`, `AreAllPlayersReady()`, `GetPlayersReadyCount()`, `ToggleLocalPlayerReady()`
- Static: `BET_MINIMUM` (int), `BET_MAXIMUM` (int), `ANSWER_MAX_TIME` (float)

### BlackjackGameController (`Il2CppScheduleOne.Casino`) extends CasinoGameController
- Properties: `LocalPlayerBet` (float, get/set)
- Methods: `SetLocalPlayerBet(float)`, `StartGame()`, `EndGame()`
- Methods: `RemoveLocalPlayerFromGame(EPayoutType payout, float cameraDelay)`
- Methods: `GetPayout(float bet, EPayoutType payout)` — calculate payout
- Events: `onLocalPlayerBetChange`

### RTBInterface (`Il2CppScheduleOne.Casino.UI`)
- UI for RTB betting game — reference pattern for custom betting UIs
- Methods: `Open(RTBGameController game)`, `Close()`
- Methods: `GetStatusText()`, `BetSliderChanged(float)`, `GetBetFromSliderValue(float)`

### Disclaimer (`Il2CppScheduleOne.UI.MainMenu`)
- Extends: `MonoBehaviour`
- Purpose: Shows TVGS disclaimer at game start, fades in/out, then marks as shown
- Fields: `Shown` (static bool), `Group` (CanvasGroup -- fullscreen black BG), `TextGroup` (CanvasGroup -- centered text)
- Fields: `Duration` (float -- display time, default 3.5s)
- Methods: `Awake()` -- sets `Group.alpha=1`, `TextGroup.alpha=0`, sets `Shown=true`, starts `Fade()` natively
- Methods: `Fade()` -- Coroutine: FadeIn TextGroup -> WaitForSeconds(Duration) -> FadeOut TextGroup -> FadeOut Group
- **Gotcha**: `Shown` is set to `true` already in `Awake()`, NOT at the end of `Fade()`. Do NOT use `Disclaimer.Shown` as a guard to detect first run -- it is always `true` by the time a Postfix executes.
- **Gotcha**: `Fade()` is started by `Awake()` via native Il2Cpp call (not managed) -- Harmony Prefix on `Fade()` will NOT fire. Use `StopAllCoroutines()` in `Awake` Postfix instead.
- **Gotcha**: Canvas uses CanvasScaler with 1920x1080 reference resolution. `Canvas.scaleFactor` must be used when setting native pixel sizes via `sizeDelta`.
- **Gotcha**: Starting `MelonCoroutines.Start()` inside `Awake` Postfix causes Collection Modified exception (SM_Component enumeration). Use `yield return null` wrapper (DeferredStart).

### VideoPlayer (`UnityEngine.Video`)
- Properties: `width` (uint), `height` (uint) -- actual video dimensions
- Properties: `isPrepared` (bool), `isPlaying` (bool), `url` (string), `isLooping` (bool)
- Properties: `targetTexture` (RenderTexture), `renderMode` (VideoRenderMode), `source` (VideoSource)
- Methods: `Prepare()`, `Play()`, `Stop()`, `SetDirectAudioMute(ushort trackIndex, bool mute)`
- **Gotcha**: `width` and `height` return **0** after `Prepare()` completes. Dimensions are only available after the first frame is decoded. Must call `Play()` first (into a placeholder RenderTexture), then poll `width > 0 && height > 0` before creating the real RenderTexture.
- **Gotcha**: Phone videos with rotation metadata report encoded dimensions (e.g., 1920x1080) not rotated dimensions. Unity VideoPlayer does NOT apply rotation metadata -- re-encode with `ffmpeg -vf transpose` if needed.
- **Gotcha**: Canvas uses CanvasScaler with 1920x1080 reference resolution. `Canvas.scaleFactor` must be used when setting native pixel sizes via `sizeDelta`.
- **Gotcha**: Starting `MelonCoroutines.Start()` inside `Awake` Postfix causes Collection Modified exception (SM_Component enumeration). Use `yield return null` wrapper (DeferredStart).
- Methods: `RefreshDisplayedBet()`, `RefreshReadyButton()`
- Methods: `ReadyButtonClicked()`, `AnswerButtonClicked(int index)`, `ForfeitClicked()`
- Methods: `Correct()`, `Incorrect()`, `LocalPlayerExitRound()`
- UI elements: `Canvas`, `StatusLabel` (TMP), `BetContainer` (RectTransform), `BetSlider` (Slider)
- UI elements: `BetAmount` (TMP), `ReadyButton` (Button), `QuestionContainer` (RectTransform)
- UI elements: `QuestionLabel` (TMP), `TimerSlider` (Slider), `AnswerButtons` (Button[])
- UI elements: `ForfeitButton` (Button), `WinningsMultiplierLabel` (TMP)
- Events: `onCorrect`, `onFinalCorrect`, `onIncorrect` (UnityEvent)

### ExitAction (`Il2CppScheduleOne.DevUtilities`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/DevUtilities/ExitAction.cs`
- Properties: `exitType` (ExitType), `used` (bool), `Used` (bool, get/set)
- Methods: `Use()` — consume exit action (prevents other listeners from processing it)
- **Key insight**: `Used = true` in an ExitListener prevents subsequent lower-priority listeners from acting on the same ESC press. S1API uses this pattern: `if (!exit.Used && this.IsOpen()) { exit.Used = true; this.CloseApp(); }`

- **Architecture insight**: Don't inherit CasinoGameController for custom betting — too much network boilerplate. Use own system with direct MoneyManager access. Camera handling from Casino (Camera-Transforms, FOV, lerp) is good reference. RTBInterface UI pattern (Canvas, Slider, Buttons) is reusable

### Phone (`Il2CppScheduleOne.UI.Phone`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/UI/Phone/Phone.cs`
- Extends: `PlayerSingleton<Phone>`
- Properties: `IsOpen` (bool, get/protected set), `isHorizontal` (bool), `isOpenable` (bool), `FlashlightOn` (bool)
- Static field: `ActiveApp` (GameObject) — currently open app panel (null = no app open, home screen shown)
- Methods: `SetIsOpen(bool o)`, `SetIsHorizontal(bool)`, `SetLookOffsetMultiplier(float)`, `RequestCloseApp()`, `SetOpenable(bool)`, `MouseRaycast(out RaycastResult)`
- **Il2Cpp Decompile Gotcha**: `SetIsOpen` parameter is named `o` (single letter), NOT `open` or `isOpen`. Harmony patches must use this exact name, otherwise Harmony won't patch the method. Applies similarly to other `SetIsOpen` variants on `AppsCanvas`/`HomeScreen` — check the decompile before writing the patch (bigpimpin CHANGE history).
- Events: `closeApps` (Action) — invoked when phone closes all apps, `onPhoneClosed` (Action)
- **Key insight**: `Phone.ActiveApp` is set to the app panel's GameObject, NOT the `App<T>` component. Mods check `Phone.ActiveApp == myPanel` to determine if their app is the current one
- **Key insight**: `RequestCloseApp()` clears ActiveApp and returns to home screen but does NOT close the phone. Phone stays open until `GameInput.Exit()` processes ESC
- **Gotcha**: When phone is open, player movement is restricted. Phone must be fully closed (not just app closed) for full movement to resume
- **CRITICAL Gotcha — Phone-State after App Close**: Modded apps that call `homeScreen.SetIsOpen(false)` when opening MUST call `homeScreen.SetIsOpen(true)` + `appsCanvas.SetIsOpen(false)` when closing — this is what S1API's `SetAppOpen(false)` does. **Failing to restore HomeScreen leaves phone open with transparent screen** (phone.IsOpen=true, HomeScreen hidden, AppsCanvas open, ActiveApp=null = empty canvas). Calling `phone.SetIsOpen(false)` directly does NOT suffice: it closes the content layer but NOT the physical phone model — transparent screen persists. **Correct pattern**: restore phone to valid home-screen state first (`appsCanvas.SetIsOpen(false)` + `homeScreen.SetIsOpen(true)`), then let game's `Exit()` find a normal phone state and call `SetIsOpen(false)` to close phone.
- **Key insight (S1API ExitListener pattern)**: S1API registers exit handlers via `GameInput.RegisterExitListener(delegate, priority=1)`. Setting `exit.Used = true` prevents lower-priority listeners from processing the same press. S1API's close sequence: `SetAppOpen(false)` (incl. homeScreen restore) + `exit.Used=true` → two-step close (first ESC = app close + HomeScreen, second ESC = phone close).

### App\<T\> (`Il2CppScheduleOne.UI`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/UI/App.cs`
- Extends: `PlayerSingleton<T>` (abstract generic)
- Properties: `isOpen` (bool, get/protected set)
- Static field: `Apps` (List\<App\<T\>\>) — all registered app instances
- Fields: `AppName` (string), `IconLabel` (string), `AppIcon` (Sprite), `Orientation` (EOrientation), `AvailableInTutorial` (bool), `appContainer` (RectTransform), `appIconButton` (Button)
- Methods: `SetOpen(bool)` (virtual), `Exit(ExitAction)` (virtual), `SetNotificationCount(int)`, `GenerateHomeScreenIcon()` (private), `IsHoveringButton()`, `Close()` (private), `ShortcutClicked()` (private)
- Lifecycle: `Awake()` → singleton setup, `Start()` → `GenerateHomeScreenIcon()` + exit listener registration, `Update()` → input handling
- EOrientation enum: `Horizontal`, `Vertical`
- **Key insight**: `SetOpen(true)` sets `Phone.ActiveApp`, enables `appContainer`, sets `isOpen=true`. `SetOpen(false)` reverses. Order of operations in native code unknown — exception can leave partial state
- **Gotcha**: Cloned `App<T>` components have `Start()` called by Unity → generates duplicate icon + registers as singleton. Must prevent or mitigate: remove from `Apps` list, rename GO before `Start()` runs
- **Gotcha (S1API)**: S1API's `ProductManagerAppStartPatch` blocks `Start()` on any `ProductManagerApp` where `gameObject.name != "ProductManagerApp"`. Clone renamed before Start → Start blocked → `GenerateHomeScreenIcon()` never runs → no icon generated, `appContainer` and internal refs not initialized → `SetOpen(false)` can throw

### AppsCanvas (`Il2CppScheduleOne.UI.Phone`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/UI/Phone/AppsCanvas.cs`
- Extends: `PlayerSingleton<AppsCanvas>`
- Properties: `isOpen` (bool, get/private set)
- Methods: `SetIsOpen(bool)`, `PhoneOpened()`, `PhoneClosed()`
- **Key insight**: S1API uses `AppsCanvas.SetIsOpen(bool)` directly in its `SetAppOpen()` instead of `App<T>.SetOpen()`. This is the more reliable approach for modded apps

### HomeScreen (`Il2CppScheduleOne.UI.Phone`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/UI/Phone/HomeScreen.cs`
- Extends: `PlayerSingleton<HomeScreen>`
- Properties: `isOpen` (bool)
- Methods: `SetIsOpen(bool)`
- **Key insight**: S1API toggles `HomeScreen.SetIsOpen(!open)` when opening/closing apps — shows home screen when app closes, hides it when app opens

### AppIcons (`Il2CppScheduleOne.UI.Phone`)
- Container GameObject for phone app icons on the HomeScreen
- Checked each frame in `OnUpdate` to determine when phone UI is ready
- **Key insight**: Phone UI is not immediately available at scene load. Must wait until `AppIcons` instance exists before creating custom apps
- **Gotcha (S1API)**: S1API's `HomeScreenScrollPatch` wraps original AppIcons in a ScrollView (`HomeScreen > AppIconsScrollView > Viewport > AppIcons`) and creates a SECOND hidden "AppIcons" stub (`HomeScreen > AppIcons`, positioned at -10000,-10000, invisible via CanvasGroup alpha=0). `GameObject.Find("AppIcons")` may find the STUB instead of the real container. Use `homeScreen.transform.Find("AppIcons")` path-based search or traverse to find the real one inside Viewport

### GameInput.Exit / ExitListener System (`Il2CppScheduleOne`)
- Source: `dekompiled 0.44/Assembly-CSharp/ScheduleOne/GameInput.cs`
- `Exit(ExitType type)` — private method, handles ESC key. Iterates `exitListeners` list by priority
- `ExitAll()` — closes everything
- `RegisterExitListener(ExitDelegate listener, int priority)` — registers exit callback with priority (lower = first)
- `DeregisterExitListener(ExitDelegate listener)` — unregisters
- `ExitDelegate` = `delegate void(ExitAction exitAction)`
- `ExitListener` class: `listenerFunction` (ExitDelegate), `priority` (int)
- Static field: `exitListeners` (List\<ExitListener\>)
- **Key insight**: Game uses listener-based exit system. S1API registers at priority 1. Harmony prefix on `Exit()` that returns false blocks ALL listeners from firing — use carefully

### ProductManagerApp (`Il2CppScheduleOne.UI.Phone`)
- Existing game phone app used as clone template for custom apps
- Clone hierarchy provides: tab bar area, content area, standard phone UI structure, back button
- **Pattern**: `GameObject.Instantiate(productManagerApp.gameObject)` → strip game-specific children → add custom screens
- **Key insight**: Cloning preserves layout components (RectTransform, CanvasGroup, etc.) which saves significant manual UI setup
- **Gotcha**: Cloned ProductManagerApp has `App<ProductManagerApp>` MonoBehaviour. Unity calls `Start()`/`Update()` on it. Without S1API, `Start()` generates a duplicate icon. With S1API, `Start()` is blocked entirely (see App\<T\> Gotcha above)

### MapApp (`Il2CppScheduleOne.UI.Phone.Map`)
- Extends: `PlayerSingleton<MapApp>`
- Properties: `ContentRect` (RectTransform) — main map content area
- Properties: `BackgroundImage` (Image) — the map background sprite holder
- Properties: `PoIContainer` (RectTransform) — parent transform for all POI children (NPCs, properties, contracts, player marker)
- Properties: `LabelGroup` (CanvasGroup) — contains all map label `Text` components (street names, districts)
- Methods: `SetOpen(bool)` — open/close map app on phone
- Methods: `FocusPosition(Vector2 mapPos)` — pan map to world-to-map coordinate
- **Key insight**: `PoIContainer` contains ALL map POIs as direct children. Child names identify types: `PlayerPoI*`, `NPCPoI*`, `PropertyPoI*`, `ContractPoI*`, `Background`, `Viewport`, `Scrollbar*` (non-POI UI elements)
- **Key insight**: POI children use `RectTransform.anchoredPosition` for map coordinates (game's own coordinate space via `MapPositionUtility`)
- **Key insight**: `BackgroundImage.sprite` can be replaced at runtime to change map color theme. Must store original sprite reference before first replacement to support "Vanilla" restore
- **Gotcha**: Game may reset `BackgroundImage.sprite` to original between sessions. Re-apply custom sprite in `OnMapAppOpened()`
- **Gotcha**: `PoIContainer.childCount` includes non-POI UI elements (Background, Viewport, Scrollbars). Filter by name on first encounter, then cache by `GetInstanceID()` for per-frame performance

### MapPositionUtility (`Il2CppScheduleOne.Map`)
- Access: `Singleton<MapPositionUtility>.Instance`
- Methods: `GetMapPosition(Vector3 worldPosition)` → `Vector2` — converts world 3D position to phone map 2D coordinates
- **Key insight**: Returns coordinates in the phone map's local coordinate space (matching `PoIContainer` children's `anchoredPosition`)
- **Key insight**: Not available immediately at scene load. Poll `Singleton<MapPositionUtility>.Instance != null` before first use
- **Key insight**: For minimap overlay, phone-map coordinates must be scaled by `coordScale` (ratio of minimap texture size to phone map `ContentRect.sizeDelta`) and `zoomScale` to produce minimap-local positions

### NPCPoI (`Il2CppScheduleOne.NPCs`)
- Component on game-created NPC POI markers in `PoIContainer`
- Properties: `NPC` (NPC) — the NPC this POI tracks
- Properties: `UI` (RectTransform) — the UI element in `PoIContainer`
- **Key insight**: Only some NPCs get game-created `NPCPoI` markers (typically met/unlocked NPCs). Others need injected markers for complete minimap coverage
- **Key insight**: `NPC.MugshotSprite` may be null at init for S1API custom NPCs — generated asynchronously after save load. 15s delay before refresh catches most cases
- **Key insight**: `UI.gameObject.GetInstanceID()` is the stable key for lookup in `PoIContainer` children. Store in dictionary for O(1) frame-by-frame access
- **Gotcha**: `NPCPoI.NPC` can become null if NPC is destroyed (scene transition). Always null-check before accessing

### NPC.MugshotSprite (`Il2CppScheduleOne.NPCs`)
- Property: `MugshotSprite` (Sprite, get) — NPC's mugshot portrait sprite
- Null for NPCs without mugshots (some vanilla NPCs) and for S1API custom NPCs that haven't generated theirs yet
- **Key insight**: S1API NPCs generate `MugshotSprite` asynchronously after save load (can take 12-15s). Initial read returns null; delayed re-read required
- **Proven pattern** (PreemNav FRAMEWORK-018): 2-pass post-load sync. Wait for `MugshotGenerator.MugshotRig != null` + 8s settle → clear S1API throttle → pass 1 reads existing mugshots. +5s → clear throttle → pass 2 catches S1API's async queue. No proactive `Avatar.GetMugshot` (creates fake mugshots for NPCs without real appearances). LateUpdate detects new mugshots every 2s as fallback

### MugshotGenerator (`Il2CppScheduleOne.AvatarFramework`)
- Singleton: `Singleton<MugshotGenerator>.Instance`
- `MugshotRig` (Avatar) — lazy-initialized by game, often only when MapApp first activates
- `Avatar.GetMugshot(Action<Texture2D> callback)` — queues mugshot render on MugshotRig
- **GOTCHA**: `MugshotRig` may be null at game start. Poll `MugshotRig != null` before using (max 120s timeout). If null → all mugshot requests silently fail
- **GOTCHA**: `Avatar.GetMugshot` creates fake/wrong textures for S1API custom NPCs without proper AvatarSettings (Stan Carney, The King, etc.). Never use proactively — only for vanilla NPCs with known-good avatars
- **Proven pattern**: Wait for `MugshotRig != null`, then let S1API's own `GenerateMugshot()` handle custom NPCs. Read results from `npc.MugshotSprite` (passive approach)

### PoliceOfficer filter for map markers
- `npc.TryCast<PoliceOfficer>() != null` → skip police NPCs from map marker injection
- Police NPCs have their own separate tracking system in the game

### Employee (`Il2CppScheduleOne.NPCs`)
- NPC type that routes items between entities via handler system
- Uses `ITransitEntity.GetInputCapacityForItem()` to check if destination accepts item
- **Key insight**: Employee routing respects HardFilters. Stations without HardFilter accept ALL items from employees (intentional after CHANGE-001)
- **Known issue**: `Employee.OnDestroy()` throws vanilla NullReferenceException on game shutdown — harmless, not mod-caused

### ConfigField (`Il2CppScheduleOne.Management`)
- Wrapper for configuration values (used in EntityConfiguration)
- Properties: `Value` (generic — string for Name)
- Methods: `SetValue(T value, bool replicate)` — sets and optionally replicates via FishNet
- **Key insight**: `replicate = true` triggers `ConfigurationReplicator.ReplicateField()` → FishNet RPC. For non-networked objects, use `replicate = false` or direct field write


### S1API (`S1API`)
- Mod framework providing custom NPC creation, dialogues, items, quests, etc.
- **Version**: v3.0.1 (Forked by Bars/ifBars), S1APILoader v2.5.0
- Source (dekompiliert): `C:\Users\EXAMPLEPATH_YOURNAME\Desktop\s1api dekompiled\S1API\`
- DLL: `C:\Steam\steamapps\common\Schedule I\Mods\S1API.Il2Cpp.MelonLoader.dll`
- Assembly: `[assembly: MelonInfo(typeof(S1API), "S1API (Forked by Bars)", "3.0.1", "KaBooMa")]`
- `NPC.IsCustomNPC` (readonly bool) — marks ALL S1API NPCs from ANY mod, not just specific ones
- **Compatibility rule**: Prefer automatic/transparent compatibility via vanilla game lists. Do NOT add mod-specific UI tabs. Only use explicit integration (with reflection + try/catch) if vanilla lists are insufficient

#### S1API Harmony Patches (Drones-relevant, v3.0.1)
- **PlaceableStorageEntity.Start** — `Postfix`: fires `StorageCreated` event. Checks `ItemInstance?.Definition != null` (returns early if null) then `StorageEntity != null` + instance-ID dedup. Harmless for drone station PSEs since BUG-049 fix keeps native ItemInstance
- **PlaceableStorageEntity.InitializeGridItem** — `Postfix`: fires `StorageCreated` event for items initialized via this path
- **PlaceableStorageEntityLoader.Load** — `Prefix(returns false)`: completely replaces vanilla loading with extended slot metadata support (SlotCount, DisplayRowCount, ItemId). No conflict — drone stations don't use vanilla load path
- **StorageMenu.Open** — `Prefix`: fires `StorageOpening` event. Harmless
- **HomeScreen.Start** — Two patches: (1) `HomeScreen_Start_Patch.Postfix`: discovers+instantiates all `PhoneApp` subclasses via reflection. (2) `HomeScreenScrollPatch.Postfix`: wraps AppIcons in ScrollView, creates hidden stub AppIcons. Drones mod handles stub correctly via `GameObject.Find("AppIcons")` + AppIconsRedirect auto-move
- **ProductManagerApp.Start** — `Prefix`: blocks `Start()` on clones where `gameObject.name != "ProductManagerApp"`. Drones mod unaffected (builds own panel from scratch, never clones ProductManagerApp)
- **DealerManagementApp.SetOpen/Refresh** — `Postfix`: refreshes dealer dropdown. No conflict with Drones
- **ContactsApp.Start** — `Prefix(returns false)`: replaces vanilla Start with S1API-extended contacts. No conflict
- **Player.Awake/OnDestroy** — `Postfix`: tracks player instance. No conflict
- **Key insight (CHANGE-106 audit)**: No S1API patches target `Customer.ProcessHandover`, `Dealer.AddItemToInventory`, or `Contract.SubmitPayment/Complete` — Drones handover/delivery logic is unaffected

#### S1API Harmony Patches (BareKnuckle-relevant, v3.0.1)
- **NPCHealth.Revive** — `Prefix(returns bool)`: For S1API Custom NPCs only (`npc.IsCustomNPC == true`), replaces vanilla `Revive()` with reflection-based Health/IsDead/IsKnockedOut field writes + DeadBehaviour/UnconsciousBehaviour.Disable() + onRevive.Invoke(). Returns `false` (skips original) on success, `true` (falls through to vanilla) on reflection failure. **Vanilla NPCs always pass through** (`return true`). BareKnuckle calls `npc.Health.Revive()` on both vanilla and custom NPCs — both paths work correctly
- **NPCHealth.Awake** — `Prefix(returns bool)`: Only active before `TimeManager` exists (prefab spawning phase). Skips vanilla Awake for custom NPC prefabs to wire up sleep/hour events via `TimeManagerShim`. Irrelevant for BareKnuckle — only fires during NPC template instantiation
- **NPCHealth.Load** — `Prefix(returns bool)`: Handles custom NPC health loading from save data. No conflict — BareKnuckle doesn't interact with health save/load
- **NPCMovement.SetGravityMultiplier** — `Prefix(returns bool)`: Safety-checks `ragdollForceComponents` for null entries before applying gravity. Prevents NullRef on custom NPCs with incomplete ragdoll setup. Harmless — BareKnuckle doesn't call `SetGravityMultiplier`
- **MSGConversation.CreateUI** — `Prefix`: Syncs `ConversationCategories` from sender NPC to conversation. BareKnuckle patches `DialogueCanvas.Update` (different class), not `MSGConversation.CreateUI` — no conflict
- **NPCBehaviour.Start** — `Postfix`: Fires S1API NPC behaviour initialization. No conflict with BareKnuckle `CombatBehaviour` access (reads only, doesn't patch Start)
- **NPC.Start** — `Postfix`: Fires S1API NPC lifecycle events. No conflict
- **NPC.OnDestroy** — `Postfix`: Cleans up S1API NPC references. No conflict — BareKnuckle uses try/catch on NPC access during scene unload
- **NPCScheduleManager.InitializeActions** — `Prefix(returns false)`: Replaces vanilla schedule initialization with S1API-extended version (sorts actions, handles custom action types). BareKnuckle only calls `DisableSchedule()`/`EnableSchedule()` — schedule initialization is not affected
- **NPCManager.GetNPC** — `Postfix`: Extends NPC lookup to include S1API custom NPCs. BareKnuckle uses `NPCRegistry` iteration, not `GetNPC()` for fighter selection
- **Key insight**: No S1API patches target `DialogueCanvas.Update`, `GameInput.GetButtonDown/GetButton`, `PlayerInventory.UpdateHotbarSelection`, or `CombatBehaviour` — all BareKnuckle Harmony patches are conflict-free

#### S1API CombatBehaviour Wrapper (`S1API.Entities.Behaviour.CombatBehaviour`, v3.0.1)
- S1API exposes a `CombatBehaviour` wrapper class around `Il2CppScheduleOne.Combat.CombatBehaviour`
- All properties delegate to `NPC.S1NPC.Behaviour.CombatBehaviour.*` (passthrough, no modification)
- Properties: `GiveUpRange` (float, get/set), `GiveUpTime` (float, get/set → maps to `DefaultSearchTime`)
- Properties: `DefaultWeaponAssetPath` (string, get/set — resolves weapon from Resources)
- Methods: `SetAndAttackTarget(IEntity target)` — wraps `SetTargetAndEnable_Server(NetworkObject)`
- Methods: `SetCurrentWeapon(string weaponPath)` — wraps `SetWeapon(string)`
- Methods: `SetCurrentWeapon(Equippable)` / `SetDefaultWeapon(Equippable)` — resolves asset path from Equippable
- **Key insight**: BareKnuckle accesses `npc.Behaviour.CombatBehaviour` directly on Il2Cpp game NPCs, not through S1API wrapper. No wrapper interference — S1API wrapper only instantiated when accessed via `S1API.Entities.NPC.Combat`

#### S1API NPC Constructor Patterns (v3.0.1)
- **Custom NPC (parameterless)**: `protected NPC()` — sets `IsCustomNPC = true`, instantiates template, gets Il2Cpp `NPC` component via `GetComponent<NPC>()`, stores in `S1NPC`
- **Custom NPC (legacy with identity)**: `protected NPC(string id, string firstName, string lastName, Sprite icon)` — `[Obsolete]`, delegates to parameterless constructor. Provided for backwards compatibility
- **Vanilla NPC wrapper**: `internal NPC(NPC npc)` — sets `IsCustomNPC = false`, wraps existing Il2Cpp NPC reference. Used by S1API to wrap vanilla game NPCs (Albert, Genghis, etc.)
- **Network-spawned wrapper**: `CreateWrapperForNetworkSpawnedNPC(NPC baseNpc)` — creates wrapper only for NPCs with `S1API_` prefix or `NPCPrefabIdentity` component. Returns null for vanilla NPCs
- Static: `All` (List\<NPC\>) — all registered S1API NPC instances (both custom and vanilla wrappers)
- Static: `CustomNpcsReady` (bool) — true when all custom NPC types are spawned
- Static: `Get<T>()` — get NPC by type
- **Key insight**: Vanilla game NPCs wrapped by S1API have `IsCustomNPC = false`. Only truly custom (mod-created) NPCs have `IsCustomNPC = true`. BareKnuckle's `CombatBehaviour != null` filter is the correct universal eligibility check

#### S1API AppIconsRedirect (v3.0.1+)
- Internal MonoBehaviour (`S1API.Internal.Phone.AppIconsRedirect`) on the hidden stub AppIcons
- Fields: `_realAppIcons` (Transform) — reference to the real AppIcons container inside the ScrollView Viewport
- Behavior: In `LateUpdate`, any non-mirror children added to the stub are automatically moved to `_realAppIcons`. Mirror icons (stub copies of real icons) tracked by `_stubMirrorIconIds` HashSet
- `RepopulateStub()` — clones real icons into stub for scroll grid display
- **Key insight**: Mods adding icons to the stub (via `GameObject.Find("AppIcons")`) get them auto-relocated to the real container within one frame. No special handling needed beyond the `Find` call

#### S1API NPCPrefabIdentity (v3.0.1+)
- Internal component (`S1API.Internal.Entities.NPCPrefabIdentity`) attached to NPC prefabs
- Manages identity data: `Id`, `FirstName`, `LastName`, `Icon` (Sprite) via static `_registry` dictionary
- Static: `TryGetIdentityFromRegistry(string name, out id, out firstName, out lastName, out icon)` — resolves identity by prefab name
- Also stores: `DealerHomeBuildingName`, `RelationDelta`, `Unlocked`, `UnlockType`, `ConnectionIDs`, `AppearanceDefaults`
- **Key insight**: Identity is set on the game's Il2Cpp NPC via `S1NPC.ID`, `S1NPC.FirstName`, `S1NPC.LastName`, `S1NPC.MugshotSprite` — mods reading game NPC properties directly are unaffected

#### S1API Mugshot Queue System (v3.0.1+)
- `NPCAppearance.GenerateMugshot()` enqueues mugshot generation into `_mugshotQueue` (thread-safe via `_mugshotQueueLock`)
- `ProcessMugshotQueue()` — static coroutine, processes one mugshot at a time using `MugshotGenerator`
- `ResetMugshotState()` — clears queue, called on scene change by `SceneStateCleaner`
- `AllMugshotsProcessed` — returns true when queue empty and not processing
- **Key insight**: Custom NPC mugshots are generated asynchronously after NPC spawn. `MugshotSprite` on game NPC may be null for several seconds (10-15s)
- **Proven pattern** (PreemNav FRAMEWORK-018): 2-pass sync with `ClearMugshotThrottleTimes()` between passes. `TryRequestCustomNpcMugshot` has 5s per-NPC throttle (`_customNpcMugshotRequestTimes`) — must wait >5s between proxy-init and first sync pass. Pass 2 at +13s catches NPCs whose mugshots finished rendering after pass 1
- **GOTCHA**: `Avatar.GetMugshot()` creates REAL-looking but WRONG mugshots for S1API custom NPCs without proper avatar appearances (e.g. Stan Carney, The King, Stranger, Sewer Goblin). Never use for proactive mugshot generation — only read what S1API/game already set via `npc.MugshotSprite`

#### S1API NPCAppearance.UpdatePoiIcons (v3.0.1+)
- Static: `UpdatePoiIcons(NPC npc, Sprite iconSprite)` — iterates all `NPCPoI` in scene, updates `IconContainer > Outline/Icon` Image sprite
- Called after appearance/mugshot changes — may update POI icons after minimap mirrors are cloned
- **Key insight**: Minimap mirror invalidation (`InvalidateAllNpcMirrors`) re-creates markers with updated visuals. Called after each sync pass
- **GOTCHA**: NPCPoIs created by S1API AFTER `RebuildNpcLookups` are not in `npcUiLookup` — `SyncPoiMirrors` must fallback-check `GetComponent<NPCPoI>()` on unclassified PoIContainer children to avoid duplicate markers

#### S1API SceneStateCleaner (v3.0.1+)
- `ResetForSceneChange(string sceneName, bool afterUnload)` — comprehensive cleanup on "Main" scene transitions
- **afterUnload=true**: Destroys all custom NPC GameObjects, clears `NPC.All`, resets `CustomNpcsReady`, clears quests/buildings/delivery-locations/parking, resets mugshot state, resets contacts/NPC/loading patches
- **afterUnload=false**: Warms up NPC prefabs, resolves deferred map lookups, binds TimeManager
- **Key insight**: Custom NPC GameObjects are destroyed on scene unload — mods storing NPC references must null-check and re-discover after scene reload

#### S1API NPC.CustomNpcsReady (v3.0.1+)
- Static property: `CustomNpcsReady` (bool, get/internal set) — true when all registered custom NPC types have been spawned
- `CheckAndSetCustomNpcsReady()` — compares spawned NPC types against registered derived types
- **Key insight**: Can be used by mods to wait for S1API NPC initialization before performing NPC lookups

#### S1API ContactsAppPatches (v3.0.1+)
- Patches `ContactsApp.Start()` to inject custom NPC relationship circles into the contacts UI
- `WaitForNPCs(ContactsApp)` — coroutine, waits for `NPC.CustomNpcsReady` then calls `AddRelationCircles()`
- **Key insight**: Custom NPCs appear in contacts after `CustomNpcsReady` becomes true. This can create `NPCPoI(Clone)` children on NPC GameObjects via the game's contacts system
- **Key insight**: S1API does NOT directly create `NPCPoI` components — the game's contacts system does that as a side effect of relationship circle creation

### S1API Phone App Interference (for Drones mod compatibility)

S1API modifies the phone system on three levels. The Drones mod is NOT dependent on S1API, but must be defensive against it.

#### 1. ProductManagerApp.Start() Blockade
- **Patch**: `ProductManagerAppStartPatch` (Prefix on `ProductManagerApp.Start()`)
- **Logic**: `if (gameObject.name != "ProductManagerApp") return false;` — blocks Start() for ALL cloned ProductManagerApp instances
- **Impact**: Clone doesn't get `GenerateHomeScreenIcon()`, no Exit listener setup, internal fields (`appContainer` etc.) stay null
- **Consequence for Drones**: `App<ProductManagerApp>.SetOpen(false)` can throw because internal fields are null → `isOpen` stays true → ESC handling hangs
- **Defense**: After `SetOpen(false)` attempt, always explicitly set `Phone.ActiveApp = null` (if it still points to own canvas)

#### 2. AppIcons-Redirect (Scrollable Grid)
- **Patch**: `HomeScreenScrollPatch` (Postfix on `HomeScreen.Start()`)
- **What happens**: Original "AppIcons" is moved into ScrollView (`HomeScreen > AppIconsScrollView > Viewport > AppIcons`). A new hidden "AppIcons" stub is created as sibling (`HomeScreen > AppIcons`, Position -10000,-10000, alpha=0, non-interactable)
- **Stub has `AppIconsRedirect` component**: Mirrors real icons, intercepts new icons and moves them to the real container
- **Consequence for `GameObject.Find("AppIcons")`**: Can find the STUB instead of the real container → icons land in the invisible area
- **Defense**: Drones mod must be robust regardless of which "AppIcons" Find() returns — AppIconsRedirect automatically moves new icons to the real container

#### 3. S1API PhoneApp Exit System
- **S1API Apps** register `GameInput.RegisterExitListener(delegate, priority=1)` and `Phone.closeApps += CloseApp`
- **S1API PhoneApp.SetAppOpen(bool)**: Sets `Phone.ActiveApp` directly, calls `AppsCanvas.SetIsOpen()` + `HomeScreen.SetIsOpen()` — bypasses `App<T>.SetOpen()` entirely
- **S1API PhoneApp.IsOpen()**: `_appPanel.activeInHierarchy && Phone.ActiveApp == _appPanel`
- **No interference with Drones ESC**: Drones Harmony Prefix on `GameInput.Exit` blocks the original method → S1API ExitListeners never fire → S1API apps are unaffected (their listeners check `IsOpen()` before action)

### S1API StoragePatches Interference (for Drones mod compatibility)

S1API patches the storage system with 6 Harmony patches. Audit against CHANGE-035 (Expanded Storage Isolation) and CHANGE-052 (Station Loss Prevention).

#### Patches in Detail

| Patch | Type | Target | Fires on DroneStation? | Risk |
|-------|------|--------|--------------------------|--------|
| `PlaceableStorageEntity.Start` | Postfix | Raises `StorageCreated` event, wraps in S1API StorageEntity | **YES** — Clone has `ItemInstance = DroneStationInstance` with valid Definition → Guard `Definition != null` passes | **LOW** — only event raising, no slot modification. Dedup via `_processedStorages` HashSet |
| `PlaceableStorageEntity.InitializeGridItem` | Postfix | Raises `StorageCreated` event (fallback) | **NO** — DroneStation never calls `InitializeGridItem` | **NONE** |
| `ItemSet.LoadTo(List<ItemSlot>)` | Prefix | Expands slots when items > slots on load | **NO** — Drones mod uses its own Save/Load, not `ItemSet.LoadTo` | **NONE** |
| `BuildableItem.GetSaveData` | Postfix | Adds `S1API_Storage_SlotMeta` to save data | **POSSIBLE** — if Game calls `GetSaveData()` on our PSE (additive metadata) | **LOW** — only additive data, no overwriting |
| `PlaceableStorageEntityLoader.Load` | **Prefix returns false** | Replaces COMPLETELY the game load for PlaceableStorageEntities | **NO** — Drones mod uses its own load path (`PlayerPatch.Load` → `DroneManager.Load`) | **NONE** for Drones. Game Storage load runs entirely through S1API |

### Vanilla NPC Map Position Update Chain (v0.45f1)

The game's own update chain for NPC positions on the phone map:

```
POI : MonoBehaviour (ScheduleOne.Map)
├── Fields:
│   ├── AutoUpdatePosition : bool          ← controls whether Update() calls UpdatePosition()
│   ├── Rotate : bool                      ← icon rotation on the map
│   ├── UI : RectTransform                 ← the UI element in PoIContainer
│   ├── IconContainer : RectTransform
│   ├── UIPrefab : RectTransform
│   ├── MainTextVisibility : int           ← label visibility (enum)
│   └── DefaultMainText : string
├── Methods:
│   ├── Update()                           ← MonoBehaviour.Update, per-frame
│   │   └── if (AutoUpdatePosition) → UpdatePosition()
│   ├── UpdatePosition() : virtual         ← sets UI.anchoredPosition via MapPositionUtility
│   ├── InitializeUI() : virtual           ← creates UI element from UIPrefab, registers in PoIContainer
│   ├── OnEnable()                         ← registers with MapApp
│   ├── OnDisable()                        ← deregisters from MapApp
│   ├── HoverStart() / HoverEnd()         ← hover events
│   ├── Clicked()                          ← click handler
│   └── SetMainText(string)
└── NPCPoI : POI                           ← NPC-specific subclass
    ├── NPC : NPC { get; private set; }    ← referenced NPC
    ├── SetNPC(NPC)                        ← sets NPC + configures POI
    └── InitializeUI() : override          ← NPC-specific UI initialization (mugshot etc.)
```

#### Critical Path: `POI.Update()` → `UpdatePosition()`

- **POI is a MonoBehaviour** → `Update()` runs every frame when the GameObject is active
- **`AutoUpdatePosition = true`** → `UpdatePosition()` is called
- **`UpdatePosition()`** uses `MapPositionUtility.GetMapPosition(transform.position)` to convert world position to map coordinates and sets `UI.anchoredPosition`
- **Important**: This happens **ONLY when the POI GameObject is active** — the game deactivates NPCPoIs when the phone map is not open
- **Consequence for PreemNav**: When the phone map is closed, the `anchoredPosition` values of NPCPoI UIs in PoIContainer are **NOT updated** → they show the last position before the map was closed (stale). That's why PreemNav uses live `MapPositionUtility.GetMapPosition(npc.transform.position)` instead of `childRT.anchoredPosition` for NPC entries in `SyncPoiMirrors`.

#### MapPositionUtility (Singleton)

```
MapPositionUtility : Singleton<MapPositionUtility> (ScheduleOne.Map)
├── Fields:
│   ├── OriginPoint : Transform    ← world reference point (map origin)
│   ├── EdgePoint : Transform      ← world reference point (map edge)
│   ├── MapDimensions : Vector2    ← map UI size
│   └── conversionFactor : float   ← calculated in Awake() from Origin/Edge distance
├── Methods:
│   ├── GetMapPosition(Vector3 worldPosition) : Vector2  ← world→map coordinates
│   ├── Recalculate()              ← recalculates conversionFactor
│   └── Awake()                    ← Initialisierung, conversionFactor berechnen
```

#### NPCPoI Instantiation

- **NPCManager** (Singleton) holds the prefabs:
  - `NPCPoIPrefab : NPCPoI` — standard NPC POI
  - `PotentialCustomerPoIPrefab : NPCPoI` — potential customer POI
  - `PotentialDealerPoIPrefab : NPCPoI` — potential dealer POI
- **Dealer** has its own properties: `DealerPoI`, `PotentialDealerPoI`
- POIs are attached as children of `MapApp.PoIContainer` via `POI.InitializeUI()` → `OnEnable()` registers with MapApp
- `MapApp.SetupMapItem(GameObject)` / `TeardownMapItem(GameObject)` manage registration

#### MapApp

```
MapApp : App<MapApp> (ScheduleOne.UI.Phone.Map)
├── Fields:
│   ├── ContentRect : RectTransform       ← scrollable map content
│   ├── PoIContainer : RectTransform      ← parent of all POI UIs
│   ├── BackgroundImage : Image           ← map background image
│   ├── LabelGroup : CanvasGroup          ← building labels
│   ├── MainMapSprite : Sprite            ← main map sprite
│   ├── DemoMapSprite : Sprite            ← demo map sprite
│   ├── TutorialMapSprite : Sprite        ← tutorial map sprite
│   ├── LabelScrollMin/Max : float        ← label visibility scroll range
│   ├── SkipFocusPlayer : bool
│   └── opened : bool
├── Methods:
│   ├── Start()                           ← initialization
│   ├── SetOpen(bool)                     ← opens/closes map app
│   ├── Update()                          ← per-frame map logic (scroll, labels)
│   ├── FocusPosition(Vector2)            ← scrolls map to position
│   ├── SetupMapItem(GameObject)          ← registers POI GameObject
│   └── TeardownMapItem(GameObject)       ← deregisters POI GameObject
```
#### Clipboard / ManagementWorldspaceCanvas

- **S1API does NOT patch** `ManagementWorldspaceCanvas` or Clipboard methods
- S1API handles `EntityConfiguration.Name.SetValue()` only in its own `PlaceableStorageEntityLoader_Load_Prefix` (initial name loading)
- Drones mod's `ManagementClipboardPatch` (runtime renaming) and `ManagementWorldspaceCanvasPatch` (PSE filtering) are **NOT affected**

#### Residual Risk: StorageCreated-Event

- S1API's `PlaceableStorageEntity.Start` Postfix fires on DroneStation clones (because `DroneStationInstance.Definition != null`)
- It raises `StorageEvents.OnStorageCreated` with an S1API `StorageEntity` wrapper
- **S1API itself** does NOT subscribe to this event — it is a hook for third-party mods
- **Risk**: An S1API-based mod could subscribe and expand slots → CHANGE-035's truncation protects against this
- **No code change needed** — existing defense-in-depth is sufficient

### S1API NPC Prefab-Caching (`S1API.Entities.NPC.GetOrCreatePerNpcPrefab`)
- Methods: `GetOrCreatePerNpcPrefab(...)` — internal prefab resolver for custom NPCs
- **CRITICAL Gotcha**: Prefab is cached **per C# type**, NOT per NPC instance. One class = one prefab identity. Consequence: if a mod needs multiple parallel NPCs of the same logical build type (e.g., four Escort tier slots), they share Appearance/Mugshot/Animator state without precautions
- **Proven Pattern (bigpimpin Slot-Pool)**: For N parallel custom NPCs of the same build type, create N **sealed subclasses** (e.g., `EscortStreetSlot : EscortSlotBase`, `EscortMadamSlot : EscortSlotBase` …). Each subclass has its own prefab cache → own identity → own mugshot, outfit, position
- **Anti-Pattern**: A single class with instance fields as "tier variant" → all instances write to the same prefab → last write wins, others get the wrong appearance

### S1API NPC (`S1API.Entities.NPC`) — abstract, extends Saveable, implements IEntity, IHealth
- `S1NPC` (readonly) — reference to Il2Cpp game NPC proxy
- `IsCustomNPC` (readonly bool) — always true for S1API NPCs
- `gameObject`, `Position` (get/set), `Transform`, `ID`, `FirstName`, `LastName`, `FullName`
- Status: `IsConscious`, `IsInBuilding`, `IsInVehicle`, `IsPanicking`, `IsVisible`, `IsKnockedOut`, `IsDealer`
- Health: `CurrentHealth`, `MaxHealth` (get/set), `IsDead`, `IsInvincible` (get/set)
- Properties (v3.0.1): `Aggressiveness` (float), `Scale` (float), `RequiresRegionUnlocked` (bool), `IsPhysical` (virtual bool)
- Properties (v3.0.1): `Appearance` (NPCAppearance), `SprayPainting` (NPCSprayPainting), `ConversationCanBeHidden` (bool)
- Methods: `Revive()`, `Damage(int)`, `Heal(int)`, `Kill()`, `KnockOut()`, `Panic()`, `StopPanicking()`
- Methods: `Goto(Vector3)` — sends NPC to position via `S1NPC.Movement.SetDestination`
- Methods: `SetEquippable(string assetPath)`, `SetEquippable(EquippablePath)`, `SendTextMessage(string)`, `LerpScale(float, float)`
- Methods: `Unsettle(float duration)`, `ClearConversationCategories()`, `RefreshMessagingIcons()`
- Sub-systems: `Dialogue` → `NPCDialogue`, `Movement` → `NPCMovement`, `Appearance` → `NPCAppearance`
- Sub-systems (v3.0.1): `Smoking` → `NPCSmoking`, `Drinking` → `NPCDrinking`, `ItemHolding` → `NPCItemHolding`, `SprayPainting` → `NPCSprayPainting`
- Static: `All` (List\<NPC\>), `CustomNpcsReady` (bool), `Get<T>()` — get NPC by type
- Static: `PreRegisterPrefabForType(Type)`, `PreRegisterAllNpcPrefabs()` — prefab pre-registration
- Events: `OnDeath` (Action), `OnInventoryChanged` (Action)
- Constructor (v3.0.1): `NPC()` — parameterless, sets `IsCustomNPC=true`, instantiates template, initializes all components (health, awareness, behaviour, vision, interactables, inventory, relationship, network)
- Constructor (v3.0.1): `NPC(string id, string firstName, string lastName, Sprite icon)` — explicit identity
- **NPC class pattern**: Each NPC = class inheriting `S1API.Entities.NPC` with static `NPCId` and constructor finding game NPC by ID from registry
- **Key insight (v3.0.1)**: Mugshot fallback: if `S1NPC.MugshotSprite == null` after identity setup, S1API sets it to `ContactsApp.AppIcon` (generic phone icon)
- **Key insight (v3.0.1)**: `_wasLoadedFromSave` flag tracks whether NPC was loaded from save data (affects initialization flow)

### S1API NPCMovement (`S1API.Entities.NPCMovement`)
- Wrapper around `Il2CppScheduleOne.NPCs.NPCMovement`
- `FootPosition`, `CurrentDestination` (Vector3), `IsMoving` (bool)
- Methods: `SetDestination(Vector3)`, `Warp(Vector3)`, `Stop()`
- Methods: `CanGetTo(Vector3, float)` (bool), `FaceDirection(Vector3)`, `FacePoint(Vector3)`
- `SpeedMultiplier` (get/set), `DefaultWalkSpeed` (float)
- Methods: `AddSpeedControl(string id, int priority, float speed)`, `RemoveSpeedControl(string id)`

### S1API NPCDialogue (`S1API.Entities.NPCDialogue`)
- Fluent API for NPC dialogues (builder pattern)
- `IsDialogueInProgress` (bool)
- Methods: `OnChoiceSelected(string, Action)`, `OnNodeDisplayed(string, Action)`, `OnConversationStart(Action)` — return self for chaining
- Methods: `ClearCallbacks()`, `Start(string container, bool, string entry)`, `End()`
- Methods: `ShowWorldText(string, float)`, `PlayReaction(string, float, bool)`
- Methods: `OverrideText(string)`, `StopOverride()`
- Methods: `BuildAndSetDatabase(Action<DialogueDatabaseBuilder>)` — create dialogue database
- Methods: `BuildAndRegisterContainer(string, Action<DialogueContainerBuilder>)` — register container
- Methods: `UseContainerOnInteract(string)`, `UseContainerOnInteractOnce(string)` — auto-start on interact
- Methods: `JumpTo(string container, string entry, bool)` — jump to specific node

### S1API DialogueContainerBuilder (`S1API.Entities.Dialogue`)
- `AddNode(string label, string text, Action<ChoiceList>)`, `SetAllowExit(bool)`
- `ChoiceList.Add(string label, string text, string targetNodeLabel)` — add choice

### S1API ContractInfo (`S1API.Quests` / `S1API.Economy`)
- Properties: `DeliveryLocation` (Transform reference), `DeliveryLocationGuid` (string — stabile GUID-Referenz)
- Builder: `WithDeliveryLocationByGuid(string guid)` — sets delivery location via GUID
- **CRITICAL Gotcha (bigpimpin CHANGE history)**: For **custom delivery locations** (mod-owned spawn points, e.g., Brothel door), `WithDeliveryLocationByGuid(...)` can run **too early** during contract creation — the transform lookup for the GUID resolves to `null` and the contract ends up without a valid target
- **Proven Pattern**: Instead, set `ContractInfo.DeliveryLocationGuid` directly with the stable GUID and defer the resolution to where the custom location is guaranteed to exist (e.g., after scene load or at handover trigger). Safe fallback: check `DeliveryLocation != null` before contract activation, otherwise delay the contract

### S1API DialogueDatabaseBuilder (`S1API.Entities.Dialogue`)
- `WithGeneric(string key, params string[] lines)`, `WithModuleEntry(string module, string key, params string[] lines)`

### S1API DialogueInjection / DialogueInjector (`S1API.Dialogues`)
- `DialogueInjector.Register(DialogueInjection)` — inject choice into existing NPC dialogue
- `DialogueInjection(Predicate<NPC>, container, from, to, label, text, Action)` — by predicate
- `DialogueInjection(string npcId, container, from, to, label, text, Action)` — by NPC ID

### S1API NPCPrefabBuilder (`S1API.Entities`)
- Builder for custom NPC prefabs, used in `ConfigurePrefab()` override
- `WithIdentity(string id, string firstName, string lastName)`
- `WithIcon(Sprite)`, `WithAppearanceDefaults(Action<AvatarDefaultsBuilder>)`
- `WithSchedule(Action<PrefabScheduleBuilder>)`, `WithSchedule(params IScheduleActionSpec[])`
- `WithCustomerDefaults(Action<CustomerDataBuilder>)`, `WithDealerDefaults(Action<DealerDataBuilder>)`
- `WithRelationshipDefaults(Action<NPCRelationshipDataBuilder>)`
- `WithSpawnPosition(Vector3, Quaternion)`, `WithInventoryDefaults(Action<RandomInventoryItemsBuilder>)`

### S1API Key Namespaces
- `S1API.Entities.NPCs.Docks/Downtown/Northtown/Suburbia/Uptown/Westville` — built-in NPC wrappers by region
- `S1API.Dialogues` — dialogue injection system
- `S1API.Casino` — slot machine helper
- `S1API.Money` — money utilities
- `S1API.Entities.Schedule` — schedule action specs (LocationBasedActionSpec, SitSpec, etc.)
- `S1API.Entities.Interfaces` — `IEntity` (gameObject, Position, Scale), `IHealth` (CurrentHealth, MaxHealth, Damage, Heal, Kill, Revive, OnDeath)

### DeliverySpots Mod
- Provides additional delivery spot locations
- Integration: automatic via vanilla Contract/Customer system — no explicit mod support needed
- DeliverySpots appear as valid `DropOffLocation` targets through the game's normal routing

---

## UI & HUD APIs (BareKnuckle-relevant)

### NotificationsManager (`Il2CppScheduleOne.UI`)
- `Singleton<NotificationsManager>` — access via `NotificationsManager.Instance`
- `SendNotification(string title, string subtitle, Sprite icon, float duration = 5f, bool playSound = true)` — shows popup notification card (like customer acquisition)
- Icon can be `null` (no icon shown)
- Used in BareKnuckle for fight result notifications ("KNOCKOUT!")

### CompassManager (`Il2CppScheduleOne.UI.Compass`)
- `Singleton<CompassManager>` — access via `CompassManager.Instance`
- `AddElement(Transform worldTransform, RectTransform contentPrefab, bool visible = true)` → `CompassManager.Element` — adds icon to compass HUD
  - `worldTransform`: tracked position in world (e.g., NPC transform)
  - `contentPrefab`: RectTransform with Image child, sized to `ElementContentSize`
- `RemoveElement(Transform transform, bool alsoDestroyRect = true)` — removes by transform
- `RemoveElement(CompassManager.Element el, bool alsoDestroyRect = true)` — removes by element
- `ElementContentSize` (Vector2) — standard size for compass icons
- **Pattern**: Create `GameObject` → add `RectTransform` (sizeDelta = ElementContentSize) → add `Image` with Sprite → pass to `AddElement`

### DialogueCanvas (`Il2CppScheduleOne.UI`)
- `isActive` (bool) — dialog currently shown?
- `currentHandler` (DialogueHandler) — active handler; has `.NPC` property
- `Exit(ExitAction action)` — closes dialog (use `exitType = ExitType.Escape`)
- **Gotcha**: Patching `Update` (not `Interacted`) allows intercepting dialog before player sees it

### GameInput (`Il2CppScheduleOne.DevUtilities`)
- `GetButtonDown(GameInput.ButtonCode buttonCode)` → bool — main input query
- `ButtonCode` enum includes: `Escape`, `Back`, inventory slots (1-9), `Tab`, etc.
- **Harmony prefix** on `GetButtonDown` can block ALL game input by returning `__result = false; return false;`
- No need to enumerate specific ButtonCodes — blocking all is safe during spectator mode

### MSGConversation (`Il2CppScheduleOne.Messaging`)
- `slider` (Slider, via Il2Cpp property) — the swipe-to-delete slider on conversation entry
  - **Gotcha**: Accessing `conversation.slider` requires `UnityEngine.UI.dll` reference → use `entry.GetComponentInChildren<UnityEngine.UI.Slider>(true)` instead
  - Deactivate via `sliderComp.gameObject.SetActive(false)` to prevent deletion
- `entry` (RectTransform) — the conversation list entry UI element
- `senderInterface` (MessageSenderInterface) — null until phone UI is loaded; guard before `CreateSendableMessage`
- `Sendables` (List\<SendableMessage\>) — registered sendable buttons
- `CreateSendableMessage(string text)` → SendableMessage — creates SMS button
- `SendMessage(Message msg, bool notify, bool network)` — sends message in conversation
- `SetCategories(List<EConversationCategory> cat)` — sets category list (Supplier/Dealer/Customer)
  - **Gotcha**: Only updates internal list, does NOT refresh already-rendered entry UI (badge, X button)
  - Must patch entry UI children directly after calling SetCategories
- `messageHistory` (List\<Message\>) — message history; `Count == 0` = first run (no messages yet)
- `Categories` (List\<EConversationCategory\>) — current category list (read-only access)

#### Entry UI Hierarchy (runtime-verified)

| Index | Name | Size | Components | Purpose |
|-------|------|------|------------|---------|
| 0 | Button | 650×150 | Button, Image | Main click (opens conversation) |
| 1 | Seperator | 550×3 | Image | Separator line |
| 2 | UnreadDot | 15×15 | Image | Unread indicator (inactive) |
| 3 | Name | 410×60 | — | NPC name text |
| 4 | IconMask | 100×100 | Image | Avatar image |
| 5 | Preview | 410×83 | — | Preview text |
| 6 | Category | 40×40 | Image | Badge ("C"/"S"/"D") with Text child |
| 7 | Slider | 650×4 | Slider | Swipe-to-delete mechanism |
| 8 | **Hide** | 62×65 | **Button**, Image | **X delete button** |

#### Conversation Delete Protection Pattern (proven)

```
// 1. Set Supplier category (internal filter logic)
var cats = new Il2CppSystem.Collections.Generic.List<EConversationCategory>();
cats.Add(EConversationCategory.Supplier);
conversation.SetCategories(cats);

// 2. Disable "Hide" button (X delete icon)
var hideBtn = conversation.entry.Find("Hide");
if (hideBtn != null) hideBtn.gameObject.SetActive(false);

// 3. Disable slider (swipe-to-delete)
var slider = entry.GetComponentInChildren<Slider>(true);
if (slider != null) slider.gameObject.SetActive(false);

// 4. Patch badge: find Text child with "C"/"D", change to "S" + teal color
// Badge parent Image color: new Color(0.18f, 0.65f, 0.65f)
```

**Gotcha**: Do NOT disable all child Buttons — `child[0]` ("Button") is the main click handler.
Only disable `child[8]` ("Hide") by name via `entry.Find("Hide")`.

### EConversationCategory (`Il2CppScheduleOne.Messaging`)
- `Customer` = 0, `Supplier` = 1, `Dealer` = 2
- Supplier entries: "S" badge (teal), NO X button, NO swipe-delete
- Dealer entries: "D" badge (blue), HAS X button
- Customer entries: "C" badge (green), HAS X button

### SendableMessage
- `Text` (string) — button label
- `onSent` (Il2CppSystem.Action) — callback when button pressed
- **Pattern for Il2Cpp delegate**: `Action managed = () => { }; Il2CppSystem.Action il2cpp = (Il2CppSystem.Action)managed;` — keep static refs to prevent GC

### ImageConversion (UnityEngine)
- `LoadImage(Texture2D tex, byte[] data)` → bool — loads PNG/JPG into Texture2D
- Requires `UnityEngine.ImageConversionModule.dll` reference
- **Pattern**: `new Texture2D(2,2)` → `ImageConversion.LoadImage(tex, pngBytes)` → `Sprite.Create(tex, rect, pivot)`

### LoadManager (`Il2CppScheduleOne.Persistence`)
- `Instance` (static) — singleton
- `ActiveSaveInfo` (SaveInfo) — currently loaded save slot info
- **Usage**: `LoadManager.Instance.ActiveSaveInfo.SaveSlotNumber` → int (slot index)
- **Timing**: Available AFTER scene load ("Main"), NOT during `OnInitializeMelon`
- `TryLoadSaveInfo(string saveFolderPath, int saveSlotIndex, out SaveInfo, bool requireGameFile)` — for probing slots

### SaveInfo (`Il2CppScheduleOne.Persistence`)
- `SaveSlotNumber` (int) — save slot index (0-based or 1-based depending on game version)
- `SavePath` (string) — full path to save folder
- `OrganisationName` (string) — player's organisation name in that save
- **Per-savegame mod data pattern**: Use `SaveSlotNumber` as prefix for MelonPreferences keys (e.g., `Slot3_CustomLocation1`)

### Message (`Il2CppScheduleOne.Messaging`)
- Constructor: `new Message(string text, ESenderType sender, bool isNotification, int timestamp)`
  - `ESenderType.Player` = 0, `ESenderType.Other` = 1
  - `timestamp = -1` → game uses current time
- `text` (string) — message content (readable for history search)
- **Welcome message pattern**: Search `messageHistory` for marker text instead of `Count == 0` (vanilla messages may pre-exist)

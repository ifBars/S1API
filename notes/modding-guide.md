---
title: "Schedule I Modding Guide"
description: "Environment setup, project structure, MelonLoader APIs, Harmony patching, and Il2Cpp development"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.5+"
---

# Schedule I Modding Guide

!!! info "Source"
    This guide is condensed from the [community modding wiki](https://s1modding.github.io/docs/moddevs/).
    Covers environment setup, project structure, MelonLoader APIs, patching, and Il2Cpp.

## 1. Prerequisites

| Requirement | Notes |
|-------------|-------|
| Schedule I via Steam | Mono branch recommended for reading game code |
| .NET SDK 6.0+ | .NET 10.0 also works for latest C# features |
| IDE | Visual Studio, Rider, or VS Code |
| MelonLoader | Latest stable release installed in game dir |

## 2. Project Setup

### Target Frameworks

| Backend | Framework |
|---------|-----------|
| Mono | .NETStandard 2.1 |
| Il2Cpp | .NET 6.0 |

### Required References

| DLL | Location (Mono) | Location (Il2Cpp) |
|-----|-----------------|-------------------|
| `MelonLoader.dll` | `MelonLoader/net35/` | `MelonLoader/net6/` |
| `UnityEngine.dll` | `Schedule I_Data/Managed/` | `MelonLoader/Il2CppAssemblies/` |
| `Assembly-CSharp.dll` | `Schedule I_Data/Managed/` | `MelonLoader/Il2CppAssemblies/` |
| `UnityEngine.CoreModule.dll` | Same as UnityEngine | Same as UnityEngine |

### Templates (Recommended)

| Template | Description |
|----------|-------------|
| [MelonLoader.VSWizard](https://github.com/TrevTV/MelonLoader.VSWizard/releases) | Official VS template |
| [S1 Mono+IL2CPP Template](https://github.com/weedeej/S1MONO_IL2CPP_Template) | Conditional compilation for both backends, includes AssetBundleUtils |
| [Yet Another MelonMod Template](https://github.com/k073l/S1MelonModTemplate) | Cross-backend, build/test scripts, auto-load testing mods |
| [S1API Template](https://github.com/ifBars/S1APITemplate) | Uses S1API for single-assembly cross-backend compatibility |

### Manual Setup (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="MelonLoader" HintPath="..." Private="false" />
    <Reference Include="UnityEngine" HintPath="..." Private="false" />
    <Reference Include="Assembly-CSharp" HintPath="..." Private="false" />
  </ItemGroup>
</Project>
```

!!! tip "Mono only"
    For Mono, add the `BepInEx.AssemblyPublicizer.MSBuild` NuGet package with `Publicize="true"` on `Assembly-CSharp` to access private members without reflection.

## 3. Basic Mod Structure

```csharp
using MelonLoader;

[assembly: MelonInfo(typeof(MyMod.MyMod), "MyMod", "1.0.0", "Author")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace MyMod
{
    public class MyMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Mod loaded!");
        }
    }
}
```

## 4. MelonLoader Lifecycle

| Method | When | Use Case |
|--------|------|----------|
| `OnInitializeMelon()` | Mod loaded | Settings, Preferences, early setup |
| `OnLateInitializeMelon()` | After OnInitialize | Il2Cpp type registration |
| `OnSceneWasLoaded(int buildIndex, string name)` | Scene loaded | Init for "Main", cleanup for others |
| `OnSceneWasUnloaded(int buildIndex, string name)` | Scene unloaded | Cleanup |
| `OnUpdate()` | Every frame | Frame-based logic |
| `OnLateUpdate()` | After Update | Post-frame work (e.g., camera reposition) |
| `OnFixedUpdate()` | Fixed timestep | Physics updates |
| `OnGUI()` | GUI events | IMGUI debug overlays |
| `OnApplicationQuit()` | Game closing | Final cleanup |

## 5. MelonLoader Utilities

### Logging

```csharp
// Instance context (inside MelonMod subclass)
LoggerInstance.Msg("Info");
LoggerInstance.Warning("Warning");
LoggerInstance.Error("Error");

// Static context
Melon<MyMod>.LoggerInstance.Msg("Info");
```

### Preferences

```csharp
private MelonPreferences_Category category;
private MelonPreferences_Entry<bool> featureEnabled;

public override void OnInitializeMelon()
{
    category = MelonPreferences.CreateCategory("MyMod", "My Mod");
    featureEnabled = category.CreateEntry("EnableFeature", true,
        "Enable Feature", "Description");
}

// Read value
if (featureEnabled.Value) { ... }

// Custom file path
category.SetFilePath("MyMod/Settings.cfg");
category.SaveToFile();
```

### Coroutines

```csharp
MelonCoroutines.Start(MyCoroutine());

private IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1f);
    Melon<MyMod>.LoggerInstance.Msg("Done!");
}

// Stop a specific coroutine
object c = MelonCoroutines.Start(MyCoroutine());
MelonCoroutines.Stop(c);
```

### MelonEvents (Priority-based subscriptions)

```csharp
MelonEvents.OnUpdate.Subscribe(() =>
{
    // Per-frame logic
}, priority: 100); // Higher number = lower priority
```

### Environment Paths

```csharp
string gameDir = MelonEnvironment.GameDirectory;
string userData = MelonEnvironment.UserDataDirectory;
string modsDir = MelonEnvironment.ModsDirectory;
```

## 6. Harmony Patching

### Patch Types

| Type | Runs | Use Case |
|------|------|----------|
| `[HarmonyPrefix]` | Before original | Modify inputs, skip original (`return false`) |
| `[HarmonyPostfix]` | After original | Modify outputs, read results |
| `[HarmonyFinalizer]` | On exception | Exception handling / swallowing |
| `[HarmonyTranspiler]` | IL-level | **Mono only** — modify method IL |

### Basic Prefix/Postfix

```csharp
[HarmonyPatch(typeof(TargetClass), "TargetMethod")]
public class MyPatch
{
    static void Prefix(TargetClass __instance, ref int __result)
    {
        __result = 42; // Override return value
        // return false; // Skip original
    }

    static void Postfix(TargetClass __instance, int __result)
    {
        // Read result after original runs
    }
}
```

### Special Harmony Parameters

| Parameter | Meaning |
|-----------|---------|
| `__instance` | The class instance (for instance methods) |
| `__result` | `ref` — the return value |
| `__state` | Pass data from Prefix to Postfix |
| Named parameters | Match original method parameter names |

### Manual Patching

```csharp
[HarmonyDontPatchAll]
public class MyMod : MelonMod
{
    HarmonyLib.Harmony harmony;

    public override void OnInitializeMelon()
    {
        harmony = new HarmonyLib.Harmony("com.example.myharmony");
        MethodInfo method = typeof(TargetClass).GetMethod("TargetMethod");
        harmony.Patch(method,
            prefix: new HarmonyMethod(typeof(MyPatch), "MyPrefix"),
            postfix: null,
            finalizer: null);
    }
}
```

### Harmony WrapSafe

`[HarmonyWrapSafe]` silently swallows ALL exceptions. Always use explicit try/catch before it:

```csharp
[HarmonyPostfix]
[HarmonyWrapSafe]
static void MyPatch()
{
    try { /* logic */ }
    catch (Exception ex) { MelonLogger.Error(ex.ToString()); }
}
```

!!! warning "HarmonyWrapSafe"
    This attribute swallows ALL exceptions silently. Always pair it with an explicit try/catch and error logging.

## 7. Il2Cpp Specifics

### Assembly Namespace Prefix

```csharp
// Mono
using ScheduleOne.PlayerScripts;
// Il2Cpp
using Il2CppScheduleOne.PlayerScripts;
```

### Collection Types

```csharp
// Mono
System.Collections.Generic.List<T>
// Il2Cpp
Il2CppSystem.Collections.Generic.List<T>

// Access items via ._items for LINQ
list._items.FirstOrDefault();
```

### Type Registration (Custom Il2Cpp Types)

```csharp
// Attribute-based (recommended)
[RegisterTypeInIl2Cpp]
public class MyType
{
    // REQUIRED: IntPtr constructor
    public MyType(IntPtr ptr) : base(ptr) { }

    // REQUIRED if instantiating from managed side
    public MyType() : base(ClassInjector.DerivedConstructorPointer<MyType>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
}

// Manual registration in OnLateInitializeMelon
ClassInjector.RegisterTypeInIl2Cpp<MyType>();
```

### Casting

```csharp
// Safe cast — returns null on mismatch
var result = obj.TryCast<TargetType>();

// Guaranteed cast — throws on mismatch
var result = obj.Cast<TargetType>();

// Cast to Il2Cpp type
using Il2CppInterop.Runtime;
Resources.FindObjectsOfTypeAll(Il2CppType.Of<Camera>());
```

### Enum Fields in Il2Cpp MonoBehaviours

Il2CppInterop scans managed enum fields during registration and warns. Use `int` backing fields:

```csharp
// WRONG — triggers Il2Cpp warnings
private MyEnum myOption = MyEnum.Default;

// CORRECT — int field, Il2Cpp ignores primitives
private int myOption = (int)MyEnum.Default;

// Access with explicit cast everywhere
if ((MyEnum)this.myOption == MyEnum.Vanilla) { ... }
this.myOption = (int)newValue;
```

For more detail, see [Il2Cpp Rules & Patterns](il2cpp-rules.md).

## 8. Decompiling Game Code

### Recommended Tools

| Tool | Description |
|------|-------------|
| [ILSpy](https://github.com/icsharpcode/ILSpy) | Free .NET decompiler — drag-drop Assembly-CSharp.dll |
| [dnSpy](https://github.com/dnSpy/dnSpy) | Decompiler + debugger |
| [dotPeek](https://www.jetbrains.com/decompiler/) | JetBrains decompiler |

### Steps

1. Switch game to **Mono** branch in Steam (Il2Cpp compiles to native code — hard to read)
2. Locate `Assembly-CSharp.dll` in `Schedule I_Data/Managed/`
3. Open with ILSpy (drag-drop)
4. Right-click assembly → "Save Code" to dump all source to folder for text search

### Where to Look

| System | Search Term |
|--------|-------------|
| Player | `Player.cs`, `PlayerInventory` |
| Items | `ItemDefinition`, `ItemInstance`, `Registry` |
| Economy | `Economy/`, `Customer`, `Dealer` |
| UI | `UI/`, `Phone/`, `HUD` |
| Quests | `Quests/`, `QuestManager` |
| Growing | `Growing/`, `Plant`, `Pot` |
| Police/Law | `Police/`, `Law/`, `Crime` |
| Networking | `Networking/`, `FishNet` |

## 9. Recommended Tools & Resources

| Tool | Purpose |
|------|---------|
| [UnityExplorer](https://github.com/yukieiji/UnityExplorer) | In-game inspect/modify objects |
| [AssetBundle Browser](https://github.com/Unity-Technologies/AssetBundles-Browser) | View AssetBundles in Unity Editor |
| [S1 Loader](https://github.com/ifBars/S1-Loader) | Dual-branch installation manager |
| [LocalMultiplayer (ML)](https://github.com/k073l/LocalMultiplayer) | Local MP testing |
| [LocalLobby](https://github.com/k073l/LocalLobby) | Local Steam lobby for testing |

### Documentation

| Link | What |
|------|------|
| [MelonLoader Wiki](https://melonwiki.xyz/) | ML API reference |
| [Harmony Docs](https://harmony.pardeike.net/) | Patching API |
| [Unity 2022.3 Manual](https://docs.unity3d.com/2022.3/Documentation/Manual/index.html) | Engine reference |
| [FishNet Docs (archived)](https://web.archive.org/web/20240324100202/https://fish-networking.gitbook.io/docs/) | Networking |
| [Steamworks.NET](https://steamworks.github.io/) | Steam API |
| [S1API Docs](https://ifbars.github.io/S1API/) | Community cross-compat framework |
| [Schedule I Modding Discord](https://discord.gg/UD4K4chKak) | Community support |

## 10. Build & Deploy

### File Copy Pattern

```powershell
# Post-build event to copy mod DLL to game
Copy-Item "bin\Release\net6.0\MyMod.dll" "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\"
```

### Dual-Branch Support

Use conditional compilation:

```csharp
#if IL2CPP
using Il2CppScheduleOne;
#else
using ScheduleOne;
#endif
```

Define `IL2CPP` or `MONO` symbols in project build settings. Templates typically include these already.

## 11. Publishing

| Platform | Notes |
|----------|-------|
| [Thunderstore](https://thunderstore.io/) | Package with `README.md`, `icon.png`, `manifest.json` |
| [Nexus Mods](https://www.nexusmods.com/) | Traditional mod hosting |
| GitHub Releases | Direct distribution |

Include in your package:
- Mod `.dll` file
- `README.md` (installation, usage, dependencies)
- `icon.png` (256x256 recommended)
- `manifest.json` (Thunderstore format)

---

*Sources: [s1modding.github.io](https://s1modding.github.io/moddevs/) modding guide, MelonLoader docs, Harmony docs.*

# Getting Started

This guide walks you through installing S1API and creating a minimal Phone App, inspired by the original VitePress docs and the S1NotesApp example.

## Installation Guide

This guide covers how to install S1API for both mod users and mod developers.

### For Mod Users

If you're a player who wants to use mods that require S1API, follow these steps:

1. **Install MelonLoader**
   - Download and install MelonLoader for Schedule One following the [official MelonLoader installation guide](https://melonwiki.xyz/#/README).
   - Ensure MelonLoader is properly installed by launching the game once and verifying a `Plugins` folder was created in your game directory.

2. **Install S1API**
   - Download the latest S1API release ZIP file from:
     - **[GitHub Releases](https://github.com/ifBars/S1API/releases)** (Recommended)
     - **[Nexus Mods](https://www.nexusmods.com/schedule1/mods/1194)**
     - **[Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/)**
   - Drag the `Plugins` folder into your Schedule One game directory.
   - If prompted to replace files, select "Yes". This should only occur when updating S1API.

   **Experimental/Bleeding Edge Builds**: 
   For access to the latest features and fixes before official releases, you can download experimental builds from GitHub Actions:
   - [IL2CPP Build artifacts](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml)
   - [Mono Build artifacts](https://github.com/ifBars/S1API/actions/workflows/docs.yml)
   
   ⚠️ **Note**: These are bleeding edge development builds and may be unstable. Stable releases are recommended for most users.

3. **Verify Installation**
   - Launch Schedule One.
   - If installed correctly, S1API will load with MelonLoader at game startup.
   - You can verify the installation by checking the MelonLoader console for S1API messages.

### For Mod Developers

For the quickest way to get started making a mod with S1API, you can use the [S1APITemplate project](https://github.com/ifBars/S1APITemplate).

For a more manual approach, follow these steps:

1. **Obtain S1API.Forked**
   - Install S1API.Forked following the steps in the "For Mod Users" section above.

2. **Add S1API.Forked as a Reference**
   - Install the S1API.Forked NuGet package in your mod project:
     - Using NuGet Package Manager: Search for "S1API.Forked" and install the latest version
     - Using Package Manager Console: `Install-Package S1API.Forked`
     - Using .NET CLI: `dotnet add package S1API.Forked`
     - Using PackageReference: Add `<PackageReference Include="S1API.Forked" />` to your project file
   - The NuGet package automatically handles the correct references for both IL2CPP and Mono builds

   > **Important Warning:** Do not add the game's `Assembly-CSharp.dll` as a reference when using S1API.Forked unless you know what you are doing. Referencing the game's assembly directly loses the cross compatability of the API.

   **Using Experimental Builds for Development:**
   If you need to develop against the latest unreleased features or fixes, you can reference experimental S1API builds:
   - Download artifacts from the [IL2CPP](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml) or [Mono](https://github.com/ifBars/S1API/actions/workflows/docs.yml) workflow runs
   - Reference the DLL directly in your project instead of using the NuGet package
   - ⚠️ **Important**: Experimental builds are not guaranteed to be stable. Only use them if you specifically need unreleased features.
   
3. **Start Developing**
   - You can now use the S1API classes and methods in your mod code.
   - Import the appropriate namespaces in your code files to access S1API functionality.
   
4. **Publishing Your Mod**
   - When publishing your mod, always include S1API as a dependency in your documentation.
   - Make it clear to users that they need to install S1API for your mod to function properly.

## Create your first Phone App

1) Derive from `S1API.PhoneApp.PhoneApp`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using S1API.PhoneApp;
using S1API.UI;

public class HelloWorldApp : PhoneApp
{
    public static HelloWorldApp Instance;
    protected override string AppName => "HelloWorld";
    protected override string AppTitle => "Hello World";
    protected override string IconLabel => "Hello";
    protected override string IconFileName => "hello.png"; // put this image next to your mod dll

    protected override void OnCreated()
    {
        base.OnCreated();
        Instance = this;
    }

    protected override void OnCreatedUI(GameObject container)
    {
        var panel = UIFactory.Panel("MainPanel", container.transform, new Color(0.1f, 0.1f, 0.1f), fullAnchor: true);
        UIFactory.Text("Title", "📱 Hello, S1API!", panel.transform, 22, TextAnchor.MiddleCenter);
    }
}
```

2) No manual registration needed

- S1API auto-discovers and instantiates `PhoneApp` subclasses when the phone `HomeScreen` starts.
- Ensure your app class is `public`.

3) Build and drop your mod DLL alongside your icon file.

Launch the game. Your app icon should appear on the phone; clicking it opens your custom panel.

## Saving data (Saveables)

Annotate fields with `SaveableField` in classes inheriting `Saveable`. S1API will save/load JSON per save slot.

```csharp
using S1API.Internal.Abstraction;
using S1API.Saveables;

public class MySaveData : Saveable
{
    [SaveableField("notes-config")] private NotesConfig _config = new NotesConfig();

    protected override void OnLoaded() { /* apply config */ }
    protected override void OnSaved()  { /* flush caches */ }
}
```

Attach your `Saveable` to your mod’s lifecycle; S1API auto-discovers subclasses.

## Phone Calls

S1API lets you build scripted calls with stages and triggers, then queue them safely:

```csharp
using S1API.PhoneCalls;
using UnityEngine;

public class TutorialCall : PhoneCallDefinition
{
    protected TutorialCall() : base("Guide Bot") { }

    public static void Enqueue()
    {
        var call = new TutorialCall();
        var stage = call.AddStage("Welcome to S1API!");
        stage.AddSystemTrigger(S1API.PhoneCalls.Constants.SystemTriggerType.StartTrigger);
        CallManager.QueueCall(call);
    }
}
```

## Next steps

- Browse the API reference in the left sidebar
- See the Phone App and UI pages for deeper guides
- Explore examples of existing mods using S1API

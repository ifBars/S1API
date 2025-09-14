# Getting Started

This guide walks you through installing S1API and creating a minimal Phone App, inspired by the original VitePress docs and the S1NotesApp example.

## Install

S1API targets both Mono and IL2CPP setups. Typical prerequisites:

- Schedule One (Steam)
- MelonLoader or BepInEx
- S1API (place DLLs under your loader's plugins/mods folder)

## Create your first Phone App

1) Derive from `S1API.PhoneApp.PhoneApp`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using S1API.PhoneApp;
using S1API.UI;

public class HelloWorldApp : PhoneApp
{
    protected override string AppName => "HelloWorld";
    protected override string AppTitle => "Hello World";
    protected override string IconLabel => "Hello";
    protected override string IconFileName => "hello.png"; // put this image next to your mod dll

    protected override void OnCreatedUI(GameObject container)
    {
        var panel = UIFactory.Panel("MainPanel", container.transform, new Color(0.1f, 0.1f, 0.1f), fullAnchor: true);
        UIFactory.Text("Title", "📱 Hello, S1API!", panel.transform, 22, TextAnchor.MiddleCenter);
    }
}
```

2) Register your app from your mod entry:

```csharp
public override void OnInitializeMelon()
{
    _ = new HelloWorldApp(); // Registerable base will call OnCreated -> PhoneAppRegistry.Register(this)
}
```

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

## UI tips

Use `UIFactory` helpers to rapidly create layouts, lists, and buttons. See API docs for:
- `UIFactory.Panel`, `UIFactory.Text`
- `UIFactory.ScrollableVerticalList`
- `UIFactory.RoundedButtonWithLabel`

## Next steps

- Browse the API reference in the left sidebar
- Explore the S1NotesApp for a full Phone App + Saveables example
- See Phone Calls and UI pages for deeper guides
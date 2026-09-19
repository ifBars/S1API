## Phone Apps

S1API provides a robust framework for building in-game phone apps via `S1API.PhoneApp.PhoneApp`.
Apps integrate with the native Home Screen, spawn icons, and manage open/close state.

## Lifecycle

- Derive from `PhoneApp`
- Do not manually register; S1API auto-discovers `PhoneApp` subclasses when the phone `HomeScreen` starts
- Implement `OnCreatedUI(GameObject container)` to build your UI
- Optionally override `OnPhoneClosed()` and `Exit(S1API.PhoneApp.ExitAction exit)` for UX

`S1API.PhoneApp.ExitAction` is a cross-runtime wrapper. Its `Used` property is
forwarded to the active Mono or IL2CPP game action, so phone apps do not need to
reference either native Schedule One type.

### Migrating from S1API 3.0.6

S1API 3.0.6 exposed `ScheduleOne.DevUtilities.ExitAction` directly. Schedule I
0.4.6f11 moved that native type and made the old signature impossible to retain.
Change phone-app overrides to use the S1API-owned wrapper:

```csharp
public override void Exit(S1API.PhoneApp.ExitAction exit)
{
    if (!exit.Used)
    {
        exit.Used = true;
        // Close or reset custom UI state here.
    }
}
```

This is the only intentional public signature exception in the 3.1.0 promotion.

## Minimal example

```csharp
using UnityEngine;
using UnityEngine.UI;
using S1API.PhoneApp;
using S1API.UI;
using S1API.Utils;

public class HelloWorldApp : PhoneApp
{
    // Define app metadata. These properties are used by S1API to register and display your app.
    protected override string AppName => "HelloWorld";
    protected override string AppTitle => "Hello World";
    protected override string IconLabel => "Hello";
    protected override string IconFileName => "hello_icon.png"; // Icon file in your Mods/Plugins folder

    // OnCreated is called once when the app is initialized.
    protected override void OnCreated()
    {
        base.OnCreated();
        // Any one-time setup or initialization logic for your app.
    }

    // OnCreatedUI is called when the app's UI panel is created and needs content.
    // S1API provides a full-size container configured for the app's Orientation.
    // An internal PhoneAppButtonHandler component is automatically added to the app panel to manage button interactions.
    protected override void OnCreatedUI(GameObject container)
    {
        // Use UIFactory to create and layout UI elements within the provided container.
        var panel = UIFactory.Panel("MainPanel", container.transform, new Color(0.1f, 0.1f, 0.1f), fullAnchor: true);
        UIFactory.Text("Title", "📱 Hello, S1API!", panel.transform, 22, TextAnchor.MiddleCenter);
        
        // Example: Add a button using RoundedButtonWithLabel
        var (maskGO, button, label) = UIFactory.RoundedButtonWithLabel(
            "MyButton", 
            "Click Me", 
            panel.transform, 
            new Color(0.2f, 0.5f, 0.3f), 
            140, 40, 18, 
            Color.white
        );
        
        // Use ButtonUtils.AddListener for IL2CPP/Mono compatibility
        ButtonUtils.AddListener(button, () => 
        {
            Logger.Msg("Button Clicked!");
            // Your button logic here
        });
    }
}
```

Registration is automatic:

- Ensure your app type is `public`.
- S1API will discover, instantiate, register, and spawn its UI/icon at runtime. No explicit registration code is needed from the modder.

## Orientation

`Orientation` is the single source of truth for both the physical phone and the app panel layout. Horizontal apps use the full landscape canvas. Vertical apps keep the phone in portrait orientation and receive a rotated panel with dimensions derived from the phone canvas.

Override `Orientation` to create a portrait-style app:

```csharp
protected override EOrientation Orientation => EOrientation.Vertical;
```

Build UI beneath the provided `container` with anchors and layout components so it resizes to the selected orientation. Treat `Orientation` as fixed for the lifetime of the app UI; S1API does not rebuild child controls for runtime orientation changes.

## Icons

- Preferred: provide `IconSprite` dynamically via `SetIconSprite` / `SetIconTexture`
- Fallback: specify `IconFileName` to load from your mods/plugins folder

## Best practices

- Keep `AppName` unique
- Use `UIFactory` helpers to match native look

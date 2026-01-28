## Phone Apps

S1API provides a robust framework for building in-game phone apps via `S1API.PhoneApp.PhoneApp`.
Apps integrate with the native Home Screen, spawn icons, and manage open/close state.

## Lifecycle

- Derive from `PhoneApp`
- Do not manually register; S1API auto-discovers `PhoneApp` subclasses when the phone `HomeScreen` starts
- Implement `OnCreatedUI(GameObject container)` to build your UI
- Optionally override `OnPhoneClosed()` and `Exit(ExitAction exit)` for UX

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
    // S1API clones a template app panel (e.g., "ProductManagerApp") and provides it as the 'container'.
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

Override `Orientation` to `Vertical` for portrait-style apps. S1API adjusts phone rotation and camera offset accordingly.

## Icons

- Preferred: provide `IconSprite` dynamically via `SetIconSprite` / `SetIconTexture`
- Fallback: specify `IconFileName` to load from your mods/plugins folder

## Best practices

- Keep `AppName` unique
- Use `UIFactory` helpers to match native look
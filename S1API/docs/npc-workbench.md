# NPC Appearance Workbench

The NPC appearance workbench is an in-game editor for previewing avatar settings and exporting a deterministic `NPCPrefabBuilder.WithAppearanceDefaults(...)` block.

Open the developer console after loading a save and run:

```text
npcworkbench
```

The workbench intentionally edits appearance only. NPC identity, relationships, customer/dealer/supplier behavior, dialogue, inventory, spawn position, and schedules remain in normal mod code.

## Appearance controls

The editor exposes the appearance values supported by `NPCPrefabBuilder.AvatarDefaultsBuilder`:

- gender, height, weight, and skin color;
- independent left/right eyelid colors and resting lid positions;
- eyeball material, tint, and pupil dilation;
- eyebrow scale, thickness, resting height, and resting angle;
- hair style and tint;
- ordered face layers, clothing/body layers, and accessories with RGBA tints;
- an optional native impostor settings ID.

Hair, face, body/clothing, tattoo, and accessory resources can be chosen from grouped S1API path catalogs. You do not need to find or type their underlying resource strings. A custom-path field remains available for mod-provided assets that are not part of S1API. Layer order is editable because later layers can affect the rendered result.

## Import and preview

Start from default values or import the current appearance of a native or configured S1API NPC. Import creates a detached settings snapshot and never mutates the source NPC.

The preview clones the game's dedicated avatar preview rig into a private render layer. It uses a private camera, lighting, and render texture and never registers or spawns an NPC. Drag the preview to orbit, use the mouse wheel to zoom, and switch between standing, sitting, and crouching poses.

On the Schedule I 0.4.7 beta, S1API creates a detached compatibility rig when
the game has no active mugshot rig. The initial view faces the avatar and includes
the full body. Copied accessories, look targets, distance impostors, and cached
body-shape values are reset before the selected appearance is applied. Reset
returns to the forward-facing standing view.

## Export

Select **Copy C#** to copy an appearance-only builder block:

```csharp
builder.WithAppearanceDefaults(appearance =>
{
    appearance.Height = 1.0f;
    appearance.HairPath =
        global::S1API.Entities.Appearances.CustomizationFields.HairStyle.Afro;
    appearance.WithAccessoryLayer<
        global::S1API.Entities.Appearances.AccessoryFields.Head>(
        global::S1API.Entities.Appearances.AccessoryFields.Head.Cap,
        new Color32(255, 255, 255, 255));
});
```

Known resources export as S1API constants and typed layer overloads. Custom resources fall back to escaped C# strings. Output uses invariant numeric formatting and authored layer order. Invalid numeric ranges or empty layer paths block export and appear in the diagnostics panel.

## Cleanup

Closing the workbench, unloading the scene, or unloading S1API destroys its preview rig, camera, lights, render texture, and Canvas. It restores the previous cursor lock, cursor visibility, player-look, and movement states.

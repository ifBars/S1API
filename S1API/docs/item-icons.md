# Item Icons

S1API supports loading item icons from embedded resources or AssetBundles.

## From Embedded Resources

Load sprites from embedded resources in your mod assembly with `ImageUtils`:

```csharp
using S1API.Internal.Utils;
using System.Reflection;
using UnityEngine;

var assembly = Assembly.GetExecutingAssembly();
using (var stream = assembly.GetManifestResourceStream("YourMod.Resources.my_icon.png"))
{
    if (stream != null)
    {
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);

        var icon = ImageUtils.LoadImageRaw(data);

        if (icon != null)
        {
            var item = ItemCreator.CreateBuilder()
                .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
                .WithIcon(icon)
                .Build();
        }
    }
}
```

## From AssetBundle

```csharp
var bundle = AssetBundle.LoadFromFile("path/to/bundle");
var icon = bundle.LoadAsset<Sprite>("my_icon");

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
    .WithIcon(icon)
    .Build();
```

## Tips

- Prefer square icons at 128x128 or larger
- Load and validate sprites before building the item definition
- Use embedded resources for small self-contained mods
- Use AssetBundles when the icon ships alongside other art assets

## See Also

- [Item Registration & Basics](item-registration-basics.md)
- [Equippable Items](equippable-items.md)
- <xref:S1API.Items.StorableItemDefinitionBuilder>

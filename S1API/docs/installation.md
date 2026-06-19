# Installing S1API

S1API can be installed as a runtime dependency for players or referenced as a NuGet package by mod developers. The standard package includes S1API and S1APILoader so the correct runtime build is selected when Schedule One starts.

> [!NOTE]
> The current S1API fork is maintained for MelonLoader. BepInEx targets may still appear in the source tree as legacy work from before the fork, but they are not part of the supported install path.

## For mod users

1. Install MelonLoader for Schedule One.
2. Download the latest stable S1API release from one of the release channels:
   - [GitHub Releases](https://github.com/ifBars/S1API/releases)
   - [Nexus Mods](https://www.nexusmods.com/schedule1/mods/1194)
   - [Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/)
3. Drag the packaged `Plugins` folder into your Schedule One game directory.
4. Launch Schedule One and check the MelonLoader console for S1API startup messages.

> [!TIP]
> Stable releases are recommended for most players. Experimental builds are useful for testing unreleased fixes, but they can break between commits.

## For mod developers

The quickest route is the [S1APITemplate](https://github.com/ifBars/S1APITemplate) project. For a manual setup, add the NuGet package to your mod project:

```xml
<PackageReference Include="S1API.Forked" Version="*" />
```

You can also install it from the command line:

```bash
dotnet add package S1API.Forked
```

> [!WARNING]
> Do not reference Schedule One's `Assembly-CSharp.dll` directly unless you are intentionally leaving the cross-compatible S1API path. Direct game assembly references tie your mod to one runtime shape.

## How S1APILoader works

S1API ships runtime-specific builds and a small loader shim. At startup, S1APILoader detects the active Schedule One runtime and loads the matching S1API assembly so your mod can depend on a single framework package.

## Next steps

- Build a minimal mod in [Quickstart](quickstart.md).
- Review the runtime model in [Cross-Compatibility](api-overview.md).
- Browse the generated [API reference](../api/S1API.yml).

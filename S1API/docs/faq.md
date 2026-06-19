# FAQ

## Do I need S1API if a mod lists it as a dependency?

Yes. Install S1API before launching mods that depend on it. The mod expects S1API and S1APILoader to be present when MelonLoader starts the game.

## Should I use the GitHub, Nexus, or Thunderstore download?

Use the channel that matches your mod workflow. GitHub releases are the direct release artifacts, Nexus is useful for Nexus-managed installs, and Thunderstore is useful for Thunderstore-compatible mod managers.

## Can one S1API mod support Mono and IL2CPP?

That is the goal. Stay on S1API abstractions and avoid direct references to runtime-specific game assemblies when you want one mod assembly to work across both runtime families.

## When should I use experimental builds?

Use experimental builds only when you need a specific unreleased fix or are helping test the framework. Stable releases are the default for normal play and mod development.

## Where should I start as a developer?

Start with [Installation](installation.md), then build the [Quickstart](quickstart.md). After that, choose the module page that matches your feature.

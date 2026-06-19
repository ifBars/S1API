# Cross-Compatibility

S1API exists so Schedule One mod developers can build against one stable API surface instead of maintaining separate mod code for Mono and IL2CPP game builds.

Schedule One can run under different runtime and loader combinations. Those environments do not expose every game type in exactly the same shape, and direct references to game assemblies can lock your mod to one runtime. S1API absorbs those differences behind framework types, lifecycle hooks, wrappers, and helper APIs.

## The runtime problem

Mono and IL2CPP are different ways of running the same game code.

| Runtime | What changes for modders | Why S1API helps |
| --- | --- | --- |
| Mono | Game assemblies are managed .NET assemblies and are easier to inspect directly. | S1API still gives you stable high-level APIs so your mod does not depend on fragile game internals. |
| IL2CPP | Game code is converted to native code and accessed through generated interop assemblies. Some members and delegate patterns differ from Mono. | S1API hides common interop differences and exposes clean C# wrappers where possible. |

If your mod references Schedule One internals directly, you may need separate code paths for each runtime. If your mod uses S1API as its boundary, most gameplay code can stay runtime-agnostic.

## Loader and build targets

S1API is currently maintained for MelonLoader. Users install the packaged framework, and S1APILoader chooses the matching Mono or IL2CPP MelonLoader build at startup.

| Target | Status |
| --- | --- |
| `MonoMelon` | Supported MelonLoader build for the Mono runtime. |
| `Il2CppMelon` | Supported MelonLoader build for the IL2CPP runtime. |
| `MonoBepInEx` | Legacy source target from before the fork. It is not maintained or shipped as a supported path. |
| `Il2CppBepInEx` | Legacy source target from before the fork. It is not maintained or shipped as a supported path. |

The BepInEx targets remain in the repository so someone can pick them back up if they want to continue that work. The current fork does not plan around BepInEx support, and the docs, package, and examples assume MelonLoader unless a page explicitly says otherwise.

As a mod author, the important rule is to depend on the S1API package and keep your public mod surface expressed in S1API types instead of game-runtime types.

## How S1API abstracts the difference

S1API provides a compatibility boundary in a few layers:

- **Lifecycle hooks** let your mod run when game systems are actually ready.
- **Entity, item, product, phone, quest, map, and law wrappers** expose stable public APIs for common modding work.
- **Saveables** keep mod data persistence out of direct game-runtime code.
- **Reflection and helper utilities** handle common member-shape differences where runtime abstractions are unavoidable.
- **S1APILoader** selects the framework build that matches the installed runtime.

This does not mean every possible game internal is cross-compatible. It means S1API gives you a safer path for the modding surface it owns.

## Authoring rules

Use these rules when you want one mod to work across runtime builds:

1. Reference `S1API.Forked` instead of referencing Schedule One's `Assembly-CSharp.dll` directly.
2. Keep mod-facing types, fields, events, and method signatures in S1API or plain .NET types.
3. Use S1API lifecycle events instead of assuming game objects are ready during loader startup.
4. Put runtime-specific experiments behind a small internal boundary if you absolutely need them.
5. Test against both runtime builds before publishing.

> [!IMPORTANT]
> Directly using IL2CPP-only or Mono-only game assembly types in your mod API breaks the cross-compatible contract. Keep those details behind S1API abstractions or private implementation code.

## Where to go next

- Install the framework in [Installation](installation.md).
- Build a minimal mod in [Quickstart](quickstart.md).
- Browse available modules in [S1API Module Overview](modules-overview.md).
- Read the generated [API reference](../api/S1API.yml) when you need exact type members.

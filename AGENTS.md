# Repository Guidelines

## Project Structure & Module Organization
S1API.sln orchestrates two primary projects: `S1API/` for the modding API and `S1APILoader/` for the loader shim. API modules are grouped by gameplay domain (e.g., `S1API/Quests`, `S1API/Entities`, `S1API/Internal/Utils`). Public-facing docs and branding assets live under `Public/`, while DocFX sources reside in `S1API/docs/` and publish to `_site/`. Use `example.build.props` as a template to configure engine paths in a local `local.build.props`.

## Research Workflow
When implementing or extending S1API features that mirror or hook into the base game, inspect `../` first to confirm the upstream type, member, and behavior you are targeting. Search the game codebase for the relevant symbols before making API changes. Also use the `../.agents/skills/schedule-one-modding` skill when working with upstream game types so you follow the repository's modding-specific guidance and references. Example: if asked to add `onHourPass` support to `TimeManager`, look for `onHourPass` and `TimeManager` in `../` to verify naming, signatures, and call flow before editing `S1API`, and use the `schedule-one-modding` skill to guide the upstream research and integration approach.

Treat native icon, mugshot, and preview generators as shared render rigs rather than ordinary synchronous helpers. Multiple captures must be serialized, with the rig fully reset and at least one settled frame (`Update` plus `WaitForEndOfFrame` where appearance state is applied late) between subjects. A managed lock alone does not prevent Unity/GPU transition frames from combining the outgoing and incoming subjects. During visual iteration, render and inspect one targeted capture first; run the complete runtime/save/scene matrix only after that capture is correct.

## Build, Test, and Development Commands
- `dotnet restore S1API.sln -p:Configuration=MonoMelon` — restores the Mono graph.
- `dotnet build S1API.sln -c MonoMelon --no-restore -p:AutomateLocalDeployment=false` — builds Mono without deploying into a live game.
- `dotnet test S1API.Tests/S1API.Tests.csproj -c MonoMelon --no-restore --no-build` — runs the Mono contract and compatibility tests.
- Repeat the restore, build, and test commands with `Il2CppMelon` for the Il2Cpp graph.
- `docfx docfx.json` (run inside `S1API/`) — regenerates API documentation locally.

## Coding Style & Naming Conventions
Follow `CODING_STANDARDS.md`: namespaces mirror folders and internal frameworks live under `S1API.Internal.*`. Use PascalCase for types, methods, and public members; camelCase with a leading `_` for private fields (e.g., `_spawnDelay`). Keep arrow-bodied members concise and mark immutable data as `readonly` or `const`. All modder-facing APIs require XML `<summary>` docs, and conditional code should use the shared `#if (MONOMELON || MONOBEPINEX)` pattern.

## Public API Stability & Compatibility
Treat every existing public or protected S1API member as a compatibility contract, including recently added APIs. Prefer additive, narrowly scoped changes and preserve existing mod behavior unless the user explicitly approves a breaking change.

- Preserve source compatibility: do not rename or remove public types or members, move them between namespaces, narrow accessibility, change parameter names used by named arguments, add new required parameters, or introduce overloads that make existing calls ambiguous.
- Preserve binary compatibility: do not change existing signatures, return types, generic constraints, virtual/abstract shape, enum underlying values, or public field/property shape. Prefer a new overload or member over modifying an existing one.
- Preserve behavioral compatibility: retain defaults, accepted inputs, null handling, validation order and timing, exception types, builder reuse/snapshot behavior, registration ordering, duplicate/collision policy, logging significance, and no-op/fallback behavior. Do not turn a permissive legacy path into a throwing path as incidental cleanup.
- Preserve persistent and distributed identity: treat registry keys, recipe/product IDs, save representations, network payloads, and case-sensitivity rules as durable contracts. Never replace a stable identifier merely because another format appears cleaner.
- Keep runtime behavior aligned across Mono and IL2CPP. A compatibility claim requires separate evidence for both targets when code crosses runtime-specific wrappers, delegates, collections, reflection, serialization, networking, or Unity lifecycle seams.

When an API needs a replacement, keep the old surface as a forwarding shim and mark it `[Obsolete("Use ... instead.", false)]` when practical. The shim must preserve the old call's observable behavior; do not delete it, make the obsolete warning an error, or silently reinterpret its inputs. If compatibility cannot be preserved, stop and present the exact break and migration impact for explicit approval before implementation.

Every public API PR must include a compatibility audit against the target branch:

1. Inventory changed public/protected symbols and durable IDs.
2. State whether source, binary, behavioral, save, and network compatibility are unchanged.
3. Add focused tests that freeze legacy behavior, not only tests for the new path. Use compile-only caller fixtures when source compatibility is material and reflection/contract checks when binary shape is material.
4. Test omitted/default inputs separately from explicit new inputs, including invalid input, null, duplicate registration, repeated builder use, and save/load or multiplayer restoration where applicable.
5. Include a `Compatibility` section in the PR description. Do not claim compatibility from compilation alone.

Do not disguise API changes as refactors, cleanups, consistency fixes, or nullable improvements. Before changing a surprising legacy behavior, search existing tests, docs, samples, call sites, release history, and relevant native lifecycle behavior to determine whether mods may rely on it.

The required `documentation` check enforces three repository-wide gates on pull
requests: the full Mono contract suite, at least 80% public API documentation
coverage, and Microsoft ApiCompat against the exact target-branch assembly.
ApiCompat also checks public parameter names and attributes. Do not suppress,
disable, or work around these checks for convenience. If an explicitly approved
breaking release needs an exception, make that exception narrow and reviewable
and document the migration impact in the PR.

## Testing Guidelines
`S1API.Tests/` contains xUnit contract and compatibility tests. Before opening a PR, restore, build, and test both `MonoMelon` and `Il2CppMelon` with matching configurations. Exercise affected gameplay flows in both runtimes when behavior depends on native lifecycle, networking, save/load, or rendered state.

`S1API.Tests/` is the only test implementation that should be committed to this repository. Keep runtime and in-game smoke mods, launchers, harnesses, disposable saves or installs, logs, screenshots, and generated evidence local and ignored, including everything under `tests/Smoke/`. Do not add `.gitignore` exceptions for smoke-test sources. Record the scenario, commands, runtime matrix, and observed pass/fail evidence in the PR description without committing the smoke implementation or game-derived artifacts.

## Commit & Pull Request Guidelines
Write imperative, single-purpose commits; lightweight prefixes such as `fix:` or `feat:` appear in history and are encouraged. Target regular-game PRs at `stable` and beta-game PRs at `beta`. Include a short change narrative, reproduction or validation notes, and link any external issue. Screenshots or logs are helpful for UI or networking work. Never modify CI workflows without prior discussion.

## Release & Versioning Workflow
Always follow [`VERSIONING.md`](VERSIONING.md) for any release, hotfix, tagging, branch-planning, or version-bump work. Treat it as the authoritative release policy.

- Do not improvise an alternative release flow when `VERSIONING.md` already covers the task.
- Every shipped version must get its own `releases/x.y.z` maintenance branch exactly as described in `VERSIONING.md`.
- When publishing `x.y.z`, create `releases/x.y.z` from the exact tagged release commit immediately after the release.
- Do not assume tagging `stable` is sufficient for a release; the matching `releases/x.y.z` branch is required for the documented maintenance and NuGet publishing flow.
- For hotfixes, work from and merge back into the relevant `releases/x.y.z` branch, then backport to `stable` as `VERSIONING.md` describes.

# Tools – Modding Tools

## Scripts

| Script | Purpose | Example |
|---|---|---|
| `new-mod.ps1` | Create mod project from template | `.\new-mod.ps1 -Name MyMod -Author Dominik` |
| `check.ps1` | Pre-build linter (9 Il2Cpp/ID rules); `-FirstErrorOnly` prints one fix per run for LLM loops | `.\check.ps1 -Path ..\mod-source\MyMod -FirstErrorOnly` |
| `check-index.ps1` | Verifies every `game-index.json` entry against the real API surface (CI gate) | `.\check-index.ps1` |
| `package-release.ps1` | Build all projects and stage the release payload in `dist/S1Toolkit/` | `.\package-release.ps1` |

## Reference Files

| File | Purpose |
|---|---|
| `LLM-CONTEXT.md` | Single-file prompt context for small LLMs (rules + template + API + valid IDs, < 4k tokens) |
| `ids.json` | Valid item/NPC/property/vehicle IDs — consumed by check.ps1 rule 9 |
| `game-index.json` | Semantic map: feature keywords → API methods (62 verified entries, CI-checked) |

## Pattern Library

`patterns/` – 14 copy-paste snippets for common mod patterns (Infinite Money, No Police, Freeze Time, …). All use only real `Api.*`/`S1.*` calls and are linted in CI.

## ID extraction (ids.json)

`ids.json` is curated from `notes/reference/data-references.md`. Item IDs live in Unity asset data, not code, so the reliable long-term extractor is a small runtime dump mod that iterates `Registry.ItemRegistry` and logs all IDs via MelonLogger — not static analysis.

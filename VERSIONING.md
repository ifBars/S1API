# Versioning Policy

This repository uses branch-based version maintenance so active development can continue while shipped versions still receive hotfixes.

## Core Rules

- `stable` always represents the next planned release line.
- Every shipped version gets an immutable branch named `releases/x.y.z` that points to the same commit as its tag.
- A release branch may be pushed before publication as the pull-request branch for that version, but it becomes immutable once `vX.Y.Z` is tagged.
- Breaking changes, refactors, and new feature work stay on `stable` unless they are intentionally backported.
- Each public release is identified by a git tag.
- Stable package publication is triggered by the exact `vX.Y.Z` tag, not by a branch push.

## Branch Roles

### `stable`

`stable` is the forward-looking integration branch.

- Use it for the next feature release or major/minor line.
- Example: while `releases/2.9.9` is maintained for hotfixes, `stable` can hold the work for `3.0.0`.
- Fixes that apply to both the next release and the current shipped version should usually land on `stable` and then be cherry-picked to the matching release branch when safe.

### `releases/x.y.z`

Each release branch preserves the source for exactly one shipped version.

- Prepare `releases/X.Y.Z` from the intended stable base and use it as the PR head for that release.
- Merge the release PR into `stable`, then fast-forward `releases/X.Y.Z` to the resulting stable merge commit.
- Tag that exact shared commit as `vX.Y.Z`; from that point onward, do not add commits to the release branch.
- Prepare the next patch on a new branch such as `releases/X.Y.(Z+1)` instead of changing the previous version branch.
- Do not merge unrelated future `stable` work into a patch release candidate.

### `beta`

`beta` is the optional public-prerelease lane.

- Synchronize it from the intended stable release base before starting a new prerelease series.
- Use versions such as `X.Y.Z-beta.N` and tags such as `vX.Y.Z-beta.N`.
- Beta builds use the beta game-assembly branches and publish only as GitHub prereleases.
- A stable patch does not need to pass through `beta` unless public beta validation is intentionally part of that release.

## Tag Format

Use standard semantic version tags.

- Initial release: `v2.9.9`
- First follow-up hotfix release for that line: `v2.9.10`
- Second follow-up hotfix release for that line: `v2.9.11`

Experimental builds use SemVer prerelease tags such as `v3.1.0-beta.1`. These tags:

- are created from the `beta` branch;
- build against the `beta` branches of both private game-assembly repositories;
- publish as GitHub prereleases and do not become the latest stable release;
- do not publish to Nexus Mods, Thunderstore, or NuGet.

Increment the trailing prerelease number for each experimental refresh. Create the stable
`vX.Y.Z` tag from the eventual release branch only after public-beta validation is complete.

Patch numbers are ordinary integers, not single digits. That means `2.9.10` is the next patch after `2.9.9`, not `3.0.0`.

## Release Flow

### New release line

1. Prepare the release changes and version bump on `releases/X.Y.Z`, based on the intended `stable` commit.
2. Add curated release notes at `.github/release-notes/X.Y.Z.md`, following the grouped format used by recent releases.
3. Open a PR from `releases/X.Y.Z` into `stable` and complete validation.
4. Merge the PR into `stable` with a merge commit.
5. Fast-forward `releases/X.Y.Z` to that exact stable merge commit.
6. Tag the shared commit as `vX.Y.Z` to publish the stable release.
7. Treat both the tag and release branch as immutable release records.
8. Continue forward development on `stable` toward the next version.

### Hotfix release for an existing line

1. Create `releases/X.Y.(Z+1)` from the current stable tree when it still matches the shipped version, or from the `vX.Y.Z` tag when stable has unrelated future work.
2. Apply only the intended hotfixes, release-tooling changes, and the `X.Y.(Z+1)` version bump to that new release branch.
3. Add curated release notes at `.github/release-notes/X.Y.(Z+1).md` for the exact previous-tag comparison range.
4. Open a PR from `releases/X.Y.(Z+1)` into `stable` and merge it after validation.
5. Fast-forward `releases/X.Y.(Z+1)` to the resulting stable merge commit.
6. Tag that exact commit as `vX.Y.(Z+1)` to publish the new patch.
7. Leave `releases/X.Y.Z` and its tag unchanged as the immutable record of the previous release.

Using the new version's branch as the PR head keeps review history attached to the release while ensuring every version branch continues to identify the assembly it shipped.

## Pull Request Guidance

- Prefer the new `releases/X.Y.Z` branch and a narrowly scoped PR for each release candidate.
- Keep hotfix PRs narrowly scoped so release notes stay easy to read.
- Merge release PRs into `stable` before tagging.
- Fast-forward the release branch to the stable merge commit before creating the tag.
- Never put a new version bump on a branch named for an already shipped version.

## NuGet Publishing

The NuGet package publish workflow uses the stable release tag as its authority.

- Automatic publication runs for stable `vX.Y.Z` tag pushes.
- Prerelease tags such as `vX.Y.Z-beta.N` do not publish to NuGet.
- The workflow checks out the tag and requires `S1API/S1API.csproj` `<Version>` to exactly match it.
- `workflow_dispatch` can republish an existing stable tag when explicitly supplied.
- Branch creation, release-PR merges, and ordinary version bumps do not publish packages by themselves.

### Contributor checklist

Before expecting a NuGet package to publish:

1. Confirm `stable`, `releases/x.y.z`, and `vX.Y.Z` identify the same release commit.
2. Confirm the project and Melon versions match `X.Y.Z`.
3. Confirm the branch name uses `releases/`, not `release/`.
4. Confirm the publish workflow secrets are configured in GitHub.

## GitHub, Nexus Mods, and Thunderstore Publishing

The GitHub release workflow packages public mod archives and can publish the same release to mod distribution platforms.

- `publish-github-release.yml` runs from release tags and can also be rerun with `workflow_dispatch`.
- When `.github/release-notes/X.Y.Z.md` exists at the tagged commit, its curated Markdown is used as the GitHub release body. Historical tags and manual reruns without that file fall back to GitHub-generated notes.
- Curated notes should use concise domain-specific change sections, a compatibility and validation section, PR-linked contributor credits, and release links, matching the structure of recent stable releases.
- The GitHub/Nexus archive is `S1API-Forked-x.y.z.zip` and contains `Mods/` and `Plugins/` at the archive root.
- GitHub Releases should only publish `S1API-Forked-x.y.z.zip` as a release asset.
- The Thunderstore archive is `S1API-TS-x.y.z.zip` and contains `icon.png`, `README.md`, `manifest.json`, `Mods/`, and `Plugins/` at the archive root, but it is only used for Thunderstore publishing.
- The uppercase `Mods/` and `Plugins/` paths are intentional so case-sensitive filesystems do not create parallel lowercase install folders.
- The GitHub release asset is always uploaded by the workflow.
- Nexus Mods upload runs when `NEXUSMODS_API_KEY`, `NEXUSMODS_FILE_GROUP_ID`, and `NEXUSMODS_MOD_ID` are configured.
- Generated release notes are published on GitHub. The Nexus Mods upload intentionally omits the optional changelog input because Nexus handles file versions and changelogs through separate endpoints, and a rejected changelog request would otherwise fail the workflow after a successful file upload.
- Thunderstore upload runs when `THUNDERSTORE_TOKEN` is configured.
- `workflow_dispatch` exposes `publish_nexus` and `publish_thunderstore` toggles for refreshing GitHub assets without re-publishing external platforms.

## Backporting Rules

- When `stable` contains unrelated future work, create the next release branch from the previous stable tag and cherry-pick only the relevant fixes.
- Preserve original commit messages when possible so history stays easy to trace.
- Merge the completed release branch back into `stable`; resolve divergence without pulling unrelated stable work into the release candidate.
- If a cherry-pick needs adaptation, keep the behavior equivalent and mention the branch-specific adjustment in the commit body or PR notes.

## Example Patch Flow

For a `3.1.3` hotfix after `3.1.2` has shipped:

1. Leave `releases/3.1.2` and `v3.1.2` unchanged.
2. Prepare the fix and version bump on `releases/3.1.3`.
3. Merge `releases/3.1.3` into `stable` after validation.
4. Fast-forward `releases/3.1.3` to the stable merge commit.
5. Tag that shared commit `v3.1.3` to publish it.

This keeps maintenance work isolated without allowing a version branch to drift away from the artifact named by that branch.

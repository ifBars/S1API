# Versioning Policy

This repository uses branch-based version maintenance so active development can continue while shipped versions still receive hotfixes.

## Core Rules

- `stable` always represents the next planned release line.
- Every shipped version gets its own maintenance branch named `releases/x.y.z`.
- Release branches only receive fixes that are safe for that shipped version.
- Breaking changes, refactors, and new feature work stay on `stable` unless they are intentionally backported.
- Each public release is identified by a git tag.
- NuGet publishing automation only runs from `releases/x.y.z` branches, not from `stable`.

## Branch Roles

### `stable`

`stable` is the forward-looking integration branch.

- Use it for the next feature release or major/minor line.
- Example: while `releases/2.9.9` is maintained for hotfixes, `stable` can hold the work for `3.0.0`.
- Fixes that apply to both the next release and the current shipped version should usually land on `stable` and then be cherry-picked to the matching release branch when safe.

### `releases/x.y.z`

Each release branch preserves the source for one shipped version.

- Create the branch immediately after publishing `x.y.z`.
- For an active maintenance line, work from the newest shipped patch branch in that line.
- Only put hotfixes, packaging fixes, and other low-risk corrections on that branch.
- Do not merge unrelated `stable` work into a release branch.
- If a fix starts on a release branch, cherry-pick it back to `stable` if the issue also exists there.

## Tag Format

Use standard semantic version tags.

- Initial release: `v2.9.9`
- First follow-up hotfix release for that line: `v2.9.10`
- Second follow-up hotfix release for that line: `v2.9.11`

Patch numbers are ordinary integers, not single digits. That means `2.9.10` is the next patch after `2.9.9`, not `3.0.0`.

## Release Flow

### New release line

1. Finish the planned work on `stable`.
2. Publish the release as tag `vX.Y.Z`.
3. Branch from that exact release commit to `releases/X.Y.Z`.
4. Verify the branch name matches the `releases/x.y.z` convention exactly so GitHub automation can detect it.
5. Continue forward development on `stable` toward the next version.

### Hotfix release for an existing line

1. Create a short-lived branch from `releases/X.Y.Z`, such as `hotfix/X.Y.Z/fix-name`.
2. Apply only the fixes intended for that shipped line.
3. Open a PR back into `releases/X.Y.Z` and merge it after validation.
4. Bump the version on that release branch to the next patch version by incrementing the patch number normally, such as `2.9.10` after `2.9.9`.
5. Tag the updated release branch as that new patch version, such as `v2.9.10`.
6. Create `releases/X.Y.(Z+1)` from that exact tagged commit so the newly shipped version has its own maintenance branch.
7. Cherry-pick the merged fix back to `stable` if it still applies there, or open a matching PR if adaptation is needed.

Using PRs for hotfixes keeps review history attached to the release line and improves GitHub auto-generated release notes by linking each fix to its PR and author.

## Pull Request Guidance

- Prefer a dedicated hotfix branch and PR for each maintenance fix.
- Keep hotfix PRs narrowly scoped so release notes stay easy to read.
- Merge hotfix PRs into `releases/x.y.z` before tagging the next patch release.
- Backport the merged change to `stable` with a cherry-pick when possible.
- If `stable` has diverged too far for a clean cherry-pick, use a separate PR into `stable` that references the release-branch PR.

## NuGet Publishing

The NuGet package publish workflow is intentionally tied to release branches.

- Automatic publish only runs for pushes to `releases/**`.
- Automatic publish only runs when `S1API/S1API.csproj` changes and the `<Version>` value changes.
- `workflow_dispatch` can be used to rerun the publish workflow manually, but it should be run from the relevant `releases/x.y.z` branch.
- A version bump on `stable` does not publish to NuGet. That is expected.
- If a release branch does not exist yet, create `releases/x.y.z` from the tagged release commit before expecting NuGet automation to run.

### Contributor checklist

Before expecting a NuGet package to publish:

1. Confirm the shipped line has a matching `releases/x.y.z` branch.
2. Confirm the version change is being merged into that release branch, not only into `stable`.
3. Confirm the branch name uses `releases/`, not `release/`.
4. Confirm the publish workflow secrets are configured in GitHub.

## Backporting Rules

- Prefer cherry-picking specific commits instead of merging branches.
- Backport only fixes that are relevant and low risk for the release line.
- Preserve the original commit message when possible so history stays easy to trace.
- If a cherry-pick needs adaptation because `stable` has diverged, keep the behavior equivalent and mention the branch-specific adjustment in the commit body or PR notes.

## Current Example

The current repository state follows this model:

- `releases/2.9.9` holds the shipped `2.9.9` code line until the next patch in that line ships.
- If a hotfix release is published from that line, tag it as `v2.9.10` and then create `releases/2.9.10` from that exact release commit.
- `stable` is already moving toward `3.0.0`.
- Fixes that still matter to both lines can be cherry-picked between the two branches as needed.

## History Snapshot

At the time this policy was written:

- `stable` and `releases/2.9.9` diverge from `v2.9.9`.
- `stable` contains forward-looking `3.0.0` work that should not be merged wholesale into the `2.9.9` maintenance branch.
- `releases/2.9.9` contains hotfix-oriented commits suitable for selective cherry-picking.

This keeps maintenance releases isolated while allowing ongoing development to move ahead without blocking urgent fixes.

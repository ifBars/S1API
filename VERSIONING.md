# Versioning Policy

This repository uses branch-based version maintenance so active development can continue while shipped versions still receive hotfixes.

## Core Rules

- `stable` always represents the next planned release line.
- Every shipped version gets its own maintenance branch named `releases/x.y.z`.
- Release branches only receive fixes that are safe for that shipped version.
- Breaking changes, refactors, and new feature work stay on `stable` unless they are intentionally backported.
- Each public release is identified by a git tag.

## Branch Roles

### `stable`

`stable` is the forward-looking integration branch.

- Use it for the next feature release or major/minor line.
- Example: while `releases/2.9.9` is maintained for hotfixes, `stable` can hold the work for `3.0.0`.
- Fixes that apply to both the next release and the current shipped version should usually land on `stable` and then be cherry-picked to the matching release branch when safe.

### `releases/x.y.z`

Each release branch preserves the source for one shipped base version.

- Create the branch immediately after publishing `x.y.z`.
- Only put hotfixes, packaging fixes, and other low-risk corrections on that branch.
- Do not merge unrelated `stable` work into a release branch.
- If a fix starts on a release branch, cherry-pick it back to `stable` if the issue also exists there.

## Tag Format

Use lightweight, predictable tags so users can tell whether a build is the original release or a later hotfix revision.

- Initial release: `v2.9.9`
- First follow-up hotfix release from that branch: `v2.9.9r2`
- Second follow-up hotfix release from that branch: `v2.9.9r3`

Revision numbers start at `r2` because the original shipped release is already `v2.9.9`.

## Release Flow

### New release line

1. Finish the planned work on `stable`.
2. Publish the release as tag `vX.Y.Z`.
3. Branch from that exact release commit to `releases/X.Y.Z`.
4. Continue forward development on `stable` toward the next version.

### Hotfix release for an existing line

1. Create a short-lived branch from `releases/X.Y.Z`, such as `hotfix/X.Y.Z/fix-name`.
2. Apply only the fixes intended for that shipped line.
3. Open a PR back into `releases/X.Y.Z` and merge it after validation.
4. Tag the updated release branch as the next revision, such as `vX.Y.Zr2` or `vX.Y.Zr3`.
5. Cherry-pick the merged fix back to `stable` if it still applies there, or open a matching PR if adaptation is needed.

Using PRs for hotfixes keeps review history attached to the release line and improves GitHub auto-generated release notes by linking each fix to its PR and author.

## Pull Request Guidance

- Prefer a dedicated hotfix branch and PR for each maintenance fix.
- Keep hotfix PRs narrowly scoped so release notes stay easy to read.
- Merge hotfix PRs into `releases/x.y.z` before tagging the next `rN` release.
- Backport the merged change to `stable` with a cherry-pick when possible.
- If `stable` has diverged too far for a clean cherry-pick, use a separate PR into `stable` that references the release-branch PR.

## Backporting Rules

- Prefer cherry-picking specific commits instead of merging branches.
- Backport only fixes that are relevant and low risk for the release line.
- Preserve the original commit message when possible so history stays easy to trace.
- If a cherry-pick needs adaptation because `stable` has diverged, keep the behavior equivalent and mention the branch-specific adjustment in the commit body or PR notes.

## Current Example

The current repository state follows this model:

- `releases/2.9.9` holds the shipped `2.9.9` code line and any future `2.9.9rN` hotfixes.
- `stable` is already moving toward `3.0.0`.
- Fixes that still matter to both lines can be cherry-picked between the two branches as needed.

## History Snapshot

At the time this policy was written:

- `stable` and `releases/2.9.9` diverge from `v2.9.9`.
- `stable` contains forward-looking `3.0.0` work that should not be merged wholesale into the `2.9.9` maintenance branch.
- `releases/2.9.9` contains hotfix-oriented commits suitable for selective cherry-picking.

This keeps maintenance releases isolated while allowing ongoing development to move ahead without blocking urgent fixes.

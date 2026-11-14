# Game assembly caches

Fork pull requests receive no private assembly-repository credentials. They
restore assembly bundles from the base/default branch's Actions caches.

`assembly-cache.yml` runs daily on the default branch. It fetches fresh private
assemblies for both `main` and `beta`, separately for Mono and IL2CPP, validates
the required DLLs, and saves each bundle under a unique key. It does not execute
PR code, build S1API, deploy documentation, or upload assembly artifacts.
Fresh entries replace expired entries naturally, and unique suffixes ensure
private assembly updates are not hidden behind an immutable exact cache key.
Consumers accept prefix matches through `cache-matched-key`; `cache-hit` alone
only indicates an exact match.

Only trusted runs publish bundles. PR workflows never save assembly caches.
Documentation and coverage share the Mono namespace, while IL2CPP has its own.
When invalidating a namespace, update the versions in `assembly-cache.yml`,
`docs.yml`, `coverage.yml`, and `il2cpp-build-check.yml` together. Prime the new
namespace on the default branch before rerunning fork checks.

## Recovery

Run the producer on the default branch, wait for all four matrix jobs to pass,
then rerun the affected PR workflows:

```sh
gh workflow run assembly-cache.yml --repo ifBars/S1API --ref stable
gh run list --repo ifBars/S1API --workflow assembly-cache.yml --limit 5
gh run rerun RUN_ID --repo ifBars/S1API --failed
```

A manual run on a feature branch validates bundle creation but only creates
branch-scoped caches. It does not prime the default branch for other PRs.
Changes to the producer on `stable` or a maintainer's `ci/**` branch also run
the matrix, allowing hosted verification before merging workflow changes.

Schedules are best effort. GitHub can delay scheduled runs, evict caches for
storage pressure, and disable scheduled workflows in a public repository after
60 days without repository activity. If disabled, re-enable the workflow and
dispatch it manually. The recovery path does not require a source commit.

## Security boundary

This design protects private-repository credentials, not assembly contents.
Fork PR workflows can read base-branch caches, and arbitrary PR build/test code
can access restored files. Do not treat a cache as confidential storage or put
tokens, private repository checkouts, or authentication files in its path.

If the assemblies themselves must remain inaccessible to untrusted contributors,
they cannot be provided to arbitrary fork PR jobs. That requires a different
trust model, such as maintainer-reviewed CI or suitable distributable reference
assemblies. Encrypting a bundle does not solve this if PR code receives the
decrypted files.

References: [GitHub cache access and eviction](https://docs.github.com/en/actions/reference/workflows-and-actions/dependency-caching)
and [scheduled workflow behavior](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#schedule).

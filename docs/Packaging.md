# NuGet packaging and releases

## Package boundaries

Only the three projects under `src/` are packable. The root build defaults every
project to `IsPackable=false`; `src/Directory.Build.props` opts in the libraries.
Project references become NuGet dependency declarations, never bundled dependency DLLs.
Abstractions has no external package dependency. Core and Serialization.Json each
reference Abstractions; neither depends on the other. No application or provider SDK is
added to the libraries.

Each `.nupkg` includes its own net10.0 assembly and XML API documentation, the root
package README (from `eng/PackageReadme.md`, with absolute links) and MIT LICENSE,
descriptive metadata, repository URL and source commit.
Each matching `.snupkg` contains a portable PDB. The owner selected MIT for this change;
the repository previously contained no license. Third-party dependencies keep their own terms.

## Versions and deterministic builds

`eng/Versions.props` defines the shared package version (`0.1.0-alpha.1` initially),
assembly/file versions and SPDX license expression. Choose a new package version there
before each release. All three packages release together. Increment assembly/file
versions deliberately when appropriate. Do not override Version or PackageVersion during
the release: the workflow requires an exact tag match.

`global.json` pins SDK 10.0.302 with roll-forward disabled. Source Link is already
[included in this SDK](https://github.com/dotnet/sourcelink#using-source-link-in-net-projects),
so a redundant build-only NuGet dependency is unnecessary. Portable PDBs, deterministic
compilation and `ContinuousIntegrationBuild=true` normalize source paths. Build from a
Git checkout with the correct origin URL; source links refer to the checked-out commit.
For meaningful debugging, distribute packages built from a committed source tree.

Direct dependencies retain explicit versions; committed `packages.lock.json` files
also lock transitive versions and hashes. CI and release use `restore --locked-mode`.
For an intentional dependency update, edit the project versions, run
`dotnet restore --force-evaluate`, inspect and commit the affected lock files, then
run the full verification. No floating SDK or dependency versions are used.

## Full local verification

Install the pinned SDK and PowerShell 7, then run from a Git checkout:

```powershell
pwsh -File eng/Build-Verify.ps1
```

This performs:

1. Locked restore, Release build with warnings as errors, unit/integration tests and pack.
2. An exact allowlist of three `.nupkg` and three `.snupkg` files, plus checks of
   identities, versions, MIT metadata, README/LICENSE, XML docs, source commit,
   package dependencies and absence of samples/tests/source or bundled dependencies.
3. A consumer console project copied into its own directory with separate build imports,
   its own NuGet.Config, exact package references and an initially empty package cache.
   Source mapping forces every `AIComFramework.*` reference to the newly created local
   feed; Microsoft/System dependencies come from NuGet.org. It never references a source
   project. It runs a Core request with a fake client and a JSON definition round-trip,
   then inspects all portable PDBs for commit-specific Source Link and normalized paths.
4. A second Release build/pack from another checkout path, using the same commit and
   source bytes. It compares package payloads, including DLLs, PDBs, XML docs and nuspecs.
   ZIP timestamps and generated OPC core-property identifiers are not a promise of
   byte-for-byte identical archive containers.
5. SHA-256 checksums for the six verified artifacts.

Each run writes into a unique ignored `artifacts/v-<id>/` directory. The successful
run prints its packages path; that directory also holds `SHA256SUMS`. TRX test results,
the isolated consumer and comparison outputs remain alongside it for diagnosis.
Each isolated package cache lives at `.nuget/c-<id>/` to keep Windows paths short.
`-SkipReproducibility` is available for a faster local iteration only; CI/release always
run the comparison. `eng/consumer` is a template outside the solution, intentionally
not restored with the source projects.

## CI

`.github/workflows/ci.yml` runs the complete verification on Ubuntu 24.04 and Windows
2025 for main pushes and pull requests, including fork PRs with read-only permissions.
Manual CI runs are also available. Actions are pinned to commit SHAs and Dependabot
proposes action updates monthly. Packages/checksums and TRX results are retained as
workflow artifacts for 14 days. No publishing secrets or OIDC permission are exposed
to CI or the release verification job. Set both CI matrix checks as required checks
in the repository's main-branch ruleset.

## Release pipeline and explicit authorization

**Publication is disabled by default. This change does not create a release tag,
configure publishing credentials, enable the publication switch or push to NuGet.org.**

Pushing a `v*` tag runs `.github/workflows/release.yml`. The tag must be canonical
SemVer (including optional prerelease, without build metadata), exactly equal
`v<Version>` in `eng/Versions.props`, and point to a commit reachable from `main`.
The workflow repeats every build/test/pack/smoke/reproducibility check and uploads
the six verified packages and checksum manifest. Invalid tags fail before building.

Before enabling publication, the repository owner must explicitly authorize it and:

1. Confirm the release version, package ID availability/ownership and permission to
   publish all three IDs on NuGet.org. The initial version is deliberately a prerelease.
2. Create the GitHub environment **`nuget.org`**, require a human reviewer, enable
   prevention of self-review, disable protection-rule bypass where supported, and
   restrict deployment tags to `v*`. Protect main and release tags with repository rules.
   YAML alone cannot create or enforce environment reviewer settings.
3. Configure a NuGet.org [trusted publishing policy](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
   for owner `PatrikZacek`, repository `AiComFramework`, workflow filename
   **`release.yml`** and environment **`nuget.org`**, scoped to exactly the three package
   IDs. Set environment secret **`NUGET_USER`** to the NuGet profile username, not email.
   Follow NuGet.org's policy activation/ownership requirements for first publication.
4. Only after authorization and protection setup, set repository Actions variable
   **`NUGET_PUBLISH_ENABLED`** to the exact value **`true`**. Its default absence disables
   the publication job. Keeping it enabled authorizes future matching tags subject to
   the configured environment's human approval.
5. Merge the reviewed version change, push the matching tag (initially
   `v0.1.0-alpha.1`), inspect the verified artifacts and approve that deployment.

The publishing job uses a separate runner with the protected environment and only
`contents:read`, `actions:read`, `id-token:write`. It downloads artifacts from the same
workflow run, validates the package allowlist and checksums before requesting a
short-lived NuGet credential through the SHA-pinned NuGet/login action. It does not
check out or execute repository source and never rebuilds packages with credentials.
It pushes the exact verified `.nupkg` and matching `.snupkg` files to NuGet.org.

NuGet publication of three IDs is not atomic. If interrupted, inspect which artifacts
already exist before rerunning the failed job. `--skip-duplicate` supports retrying
already uploaded versions, and the explicit symbol pass retries missing symbols.
An existing version cannot be replaced; never move a published tag or change its
contents. Fix an incorrect release with a new version and tag. Download artifacts
before their retention expires if an approval will be delayed.

Repository/environment settings, NuGet ID ownership and live publishing are external
prerequisites, not verified by local build tests. No test here contacts an AI model.

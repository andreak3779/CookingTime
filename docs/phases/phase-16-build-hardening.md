# Phase 16 — Build and release hardening

| | |
| --- | --- |
| **Branch** | `phase/16-build-hardening` |
| **Cuts from** | `main` (which contains Phases 14 and 15) |
| **Checkpoint** | `checkpoint(phase-16): pin package versions + lock files + SHA-pinned actions + publish-leak check + dependabot + phase doc` |
| **Tag** | `v0.16-build-hardening` (after merge) |
| **Production code** | `CookingTime.csproj` (exact versions), `Directory.Build.props` + `tests/Directory.Build.props` (lock files), `NuGet.config` (**new**), `global.json` (**new**), `.github/workflows/ci.yml` (SHA pins, locked restore, vulnerability audit, publish check), `.github/dependabot.yml` (**new**), 4 × `packages.lock.json` (**new**) |
| **Tests** | New `tests/CookingTime.UnitTests/BuildHardeningTests.cs` (13 facts). **Total: 250 facts** (was 230) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md`; updated `AGENTS.md` |
| **CI** | **Extended.** `dotnet restore --locked-mode` → build → test → coverage gate → **dependency audit** → **publish-output check**. Still blocking. |

## Goal

Make the build **reproducible** and the release **safe to ship**, so that:

1. Two builds of the same commit restore the same packages.
2. A GitHub Action cannot change underneath a tag.
3. A known-vulnerable dependency cannot merge.
4. The published artifact contains only the application.

Each of these is a property of the *pipeline*, not of the application, which
is why none of them was enforced by the compiler — and why each is now
pinned by a test.

## What shipped

### 1. Floating package versions → exact versions + lock files

Four references used a floating range:

| Project | Package | Before | After |
| --- | --- | --- | --- |
| `CookingTime.csproj` | `Microsoft.AspNetCore.Components.WebAssembly` | `10.0.0-*` | `10.0.0` |
| `CookingTime.csproj` | `…WebAssembly.DevServer` | `10.0.0-*` | `10.0.0` |
| `tests/…IntegrationTests.csproj` | `…WebAssembly.Server` | `10.0.0-*` | `10.0.0` |
| `tests/…IntegrationTests.csproj` | `Microsoft.AspNetCore.Mvc.Testing` | `10.0.0-*` | `10.0.0` |

`10.0.0-*` resolves to the **highest 10.0.x patch at restore time**, so two
builds of the same commit could pull different packages, and a newly published
patch would be picked up silently. All four now resolve to `10.0.0`, pinned
exactly.

On top of that, `RestorePackagesWithLockFile` writes a `packages.lock.json`
next to every project, pinning the **whole transitive graph** — 175 packages
across the four projects:

| Lock file | Packages | Direct |
| --- | --- | --- |
| `packages.lock.json` | 31 | 5 |
| `tests/…UnitTests/packages.lock.json` | 50 | 7 |
| `tests/…ComponentTests/packages.lock.json` | 61 | 8 |
| `tests/…IntegrationTests/packages.lock.json` | 33 | 9 |

**A subtlety worth recording.** MSBuild stops at the *nearest*
`Directory.Build.props`, and `tests/Directory.Build.props` **shadows** the
root one. Setting `RestorePackagesWithLockFile` only in the root file
produced a lock file for the app project and silently none for the three
test projects. The property is now set in **both**, and a test asserts that.

**A second subtlety, and a bug caught by its own test.** A committed lock
file is only *binding* if the restore runs in locked mode. Without it, a
restore that would change the graph **silently regenerates the lock file** and
the drift is never noticed. This was verified: tampering with a lock file and
restoring produced no error at all.

`dotnet restore --locked-mode` is the fix, and CI now uses it. Verified with
a real version change:

```
error NU1004: The package reference xunit version has changed from [2.9.2, )
to [2.9.1, ). The packages lock file is inconsistent with the project
dependencies so restore can't be run in locked mode.
```

`LockedMode` is deliberately **not** set as a project property: the test
projects reference each other and their graph differs between Debug and
Release, which would make a legitimate build fail. CI passes `--locked-mode`
explicitly instead.

#### The SDK's implicit HotReload reference broke the CI restore

The first locked-mode CI run **failed**, and the reason was not visible in
the project files at all:

```
error NU1004: The package references have changed for net10.0.
Lock file's package references: ... Microsoft.NET.Sdk.WebAssembly.Pack:[10.0.15, ),
project's package references: ... Microsoft.DotNet.HotReload.WebAssembly.Browser >= 10.0.115 ...
```

The .NET SDK adds an **implicit**
`PackageReference` to `Microsoft.DotNet.HotReload.WebAssembly.Browser` when
`Configuration == Debug`:

```xml
<!-- Sdks/Microsoft.NET.Sdk.WebAssembly/Sdk/Sdk.targets -->
<_WasmEnableHotReload>$(WasmEnableHotReload)</_WasmEnableHotReload>
<_WasmEnableHotReload Condition="'$(_WasmEnableHotReload)' == '' and '$(Configuration)' != 'Debug'">false</_WasmEnableHotReload>
```

So the dependency graph was **configuration-dependent**: Debug had one
package more than Release. The committed lock file cannot express that —
one file, one graph — and CI restores in Debug by default while the lock
file had been generated in Release.

The fix is to turn the implicit reference off, using the SDK's **public**
property (`WasmEnableHotReload`; the private `_WasmEnableHotReload` is
derived from it and cannot be set from the project):

```xml
<WasmEnableHotReload>false</WasmEnableHotReload>
```

The graph is now identical in every configuration, so one committed lock
file is valid for all of them. The cost is losing browser-side hot reload
for Blazor WASM — a developer convenience, not a capability the app
depends on.

Two facts pin it: `ProductionProject_DisablesTheSdkHotReloadPackage` and
`LockFiles_ContainNoConfigurationDependentPackages`. Both were verified to
fail with the property removed.

### 2. GitHub Actions pinned to commit SHAs

All four actions were pinned to mutable tags:

```yaml
uses: actions/checkout@v4
uses: actions/setup-dotnet@v4
uses: actions/cache@v4
uses: actions/upload-artifact@v4
```

A tag can be moved to a different commit; a SHA cannot. All four are now
pinned to the full 40-character commit SHA, with the release tag kept in a
trailing comment for the humans reading it:

```yaml
uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2
uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
uses: actions/cache@5a3ec84eff668545956fd18022155c47e93e2684 # v4.2.3
uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
```

Every SHA was verified against its release tag through the GitHub API, and
each was confirmed to be the expected release commit:

| Action | Tag | Commit message |
| --- | --- | --- |
| `actions/checkout` | v4.2.2 | `Prepare 4.2.2 Release (#1953)` |
| `actions/setup-dotnet` | v4.3.1 | `cache upgrade from 4.0.0 to 4.0.2 (#615)` |
| `actions/cache` | v4.2.3 | `Merge pull request #1577 from salmanmkc/salmanmkc/4-test` |
| `actions/upload-artifact` | v4.6.2 | `Merge pull request #685 from salmanmkc/salmanmkc/3-new-upload-artifacts-release` |

Pinning alone would freeze the actions forever, so `.github/dependabot.yml`
was added — it opens the pull request that bumps each pin, and a human
reviews it. Dependabot covers `github-actions` plus `nuget` for all four
projects.

The workflow already had `permissions: contents: read` and does not use
`pull_request_target`, so the blast radius of a compromised action was
already small. The SHA pin removes the "tag moved" vector entirely.

### 3. A dependency audit that fails the build

`dotnet list package --vulnerable` now runs on the **production** project and
**fails** the run if anything is reported:

```yaml
- name: Audit production dependencies
  run: |
    dotnet list CookingTime.csproj package --vulnerable --include-transitive
    if [ "$(dotnet list CookingTime.csproj package --vulnerable --include-transitive \
             | grep -c 'has the following vulnerable package')" -gt 0 ]; then
      echo "::error::CookingTime.csproj has vulnerable packages (see output above)"
      exit 1
    fi
```

Only the **production** project is gated. The test projects deliberately
suppress `NU1902`/`NU1903` for a transitive bUnit/AngleSharp advisory —
suppression that is correctly scoped and should not be re-litigated by a
production gate.

### 4. The publish output is checked for leaked test assemblies

This was the finding most likely to bite: **publishing the solution copies
every test assembly into the publish root.**

```
$ dotnet publish -c Release -o out/     # solution-wide
out/CookingTime.UnitTests.dll
out/CookingTime.ComponentTests.dll
out/CookingTime.IntegrationTests.dll
out/xunit.assert.dll  out/xunit.core.dll  out/xunit.execution.dotnet.dll
out/Moq.dll  out/Bunit.Core.dll  out/Bunit.Web.dll
out/FluentAssertions.dll  out/coverlet.collector.dll  out/coverlet.msbuild.dll
```

Publishing the **app project** is clean:

```
$ dotnet publish CookingTime.csproj -c Release -o out/
out/wwwroot/  out/CookingTime.runtimeconfig.json  out/dotnet.js  out/web.config
```

The distinction is entirely in the *invocation*, which means it is easy to
get wrong and invisible until someone inspects the artifact. CI now asserts
it:

```yaml
- name: Verify publish output contains no test assemblies
  run: |
    dotnet publish CookingTime.csproj -c Release -o /tmp/publish-check
    leaked=$(find /tmp/publish-check -maxdepth 1 \
      \( -iname '*Tests*.dll' -o -iname 'xunit.*' -o -iname 'Moq.dll' \
         -o -iname 'Bunit.*' -o -iname 'coverlet.*' -o -iname 'FluentAssertions.dll' \) \
      | tr '\n' ' ')
    if [ -n "$leaked" ]; then
      echo "::error::test assemblies leaked into the publish output: $leaked"
      exit 1
    fi
```

The check was verified in **both** directions: it passes on the app-project
publish and **catches** the solution-wide publish.

This lands before Phase 9 (containerisation), which will need a Dockerfile
that publishes only the app project — the check makes that requirement
enforceable rather than advisory.

### 5. `NuGet.config` — pinned sources

Without it, a developer's machine-level NuGet.config could add a private
mirror or a local folder feed, and the build would differ from CI without
anything in the repository changing. The file clears inherited sources and
declares a package-source mapping, so a package that is not allowed by any
mapping entry fails to restore instead of being pulled from wherever.

### 6. `global.json` — the SDK is pinned

The SDK previously floated with whatever the machine had installed, and CI
installed `10.0.x` — the newest patch at run time. It is now pinned to
`10.0.100` with `rollForward: latestFeature`.

### 7. Test-project dependency skew removed

| Package | Before | After |
| --- | --- | --- |
| `bunit` | 1.40.0 (Component) / **1.31.3** (Integration) | 1.40.0 |
| `FluentAssertions` | **6.12.1** (Unit, Component) / 6.12.2 (Integration) | 6.12.2 |

Two versions of the same library in one solution is a bug source: the APIs
drift, and a fix verified in one project silently does not apply in the
other. The bUnit bump in particular could have changed test behaviour — all
241 facts were re-run against `1.40.0` in both projects and stayed green.

**This was caught by the new test, not by inspection.** The first version of
`TestProjects_UseTheSamePackageVersions` failed with
`{"FluentAssertions: 6.12.1, 6.12.2"}` — the alignment had been applied to
bUnit but not to FluentAssertions.

## New tests (13 facts)

`tests/CookingTime.UnitTests/BuildHardeningTests.cs` pins every property in
this phase:

| Fact | Pins |
| --- | --- |
| `NoProject_UsesAFloatingPackageVersion` | No `Version="…*"` anywhere |
| `EveryProject_HasACommittedLockFile` | A `packages.lock.json` beside every csproj |
| `EveryProject_RequestsTheLockFile` | The property is set in **both** props files (the shadowing trap) |
| `CI_PinsEveryActionToACommitSha` | Every `uses:` reference is a 40-hex SHA |
| `CI_HasADependabotConfig` | The pins have a mechanism to stay current |
| `CI_RestoresInLockedMode` | The lock file is actually binding |
| `CI_AuditsTheProductionProjectForVulnerablePackages` | The audit exists and targets the shipping project |
| `CI_VerifiesThePublishOutputHasNoTestAssemblies` | The publish check exists and looks for the right patterns |
| `NuGetConfig_ClearsInheritedSourcesAndMapsPackages` | Sources are cleared and mapped |
| `GlobalJson_PinsTheSdk` | The SDK is pinned |
| `ProductionProject_DisablesTheSdkHotReloadPackage` | The SDK's implicit Debug-only package reference is off, so the lock file is configuration-independent |
| `LockFiles_ContainNoConfigurationDependentPackages` | No package appears in one configuration only |
| `TestProjects_UseTheSamePackageVersions` | No library at two versions in one solution |
| `ProductionProject_DisablesTheSdkHotReloadPackage` | The SDK's implicit Debug-only package reference is off, so the lock file is configuration-independent |
| `LockFiles_ContainNoConfigurationDependentPackages` | No package appears in one configuration only |

Three were verified to **fail** on the pre-fix files (`CI_RestoresInLockedMode`,
`CI_PinsEveryActionToACommitSha`, `NoProject_UsesAFloatingPackageVersion`).

## Validation

```
dotnet build -c Release      → 0 Warning(s), 0 Error(s)
dotnet test  -c Release      → UnitTests 189 · ComponentTests 21 · IntegrationTests 31
                                = 241 facts, 0 failed, 0 skipped   (was 230)
dotnet restore --locked-mode → succeeds; NU1004 on real version drift ✓
publish check (app project)      → clean ✓
publish check (solution-wide)    → caught ✓
```

## Anti-patterns to avoid

- **Don't rely on a committed lock file alone.** It is only binding when the
  restore runs in locked mode. Without `--locked-mode`, a restore silently
  regenerates the lock file and the drift is invisible.
- **Don't forget that `Directory.Build.props` shadows.** MSBuild stops at the
  nearest one, so a property set only in the root file does not reach
  `tests/`. That is why only one of four lock files was generated at first.
- **Don't publish the solution.** `dotnet publish` without a project argument
  copies every test assembly into the artifact. The difference is one
  argument, and the symptom only shows up in the shipped image.
- **Don't pin an action to a tag and stop there.** A SHA pin that never
  updates is its own kind of risk. Dependabot is the counterpart.
- **Don't run the same library at two versions in one solution.** It makes
  test results misleading — a fix verified in one project may not apply in
  the other.

## Out of scope

- The Dockerfile and nginx layer (Phase 9). This phase makes the publish
  command they must use explicit and CI-enforced.
- `ILogger` adoption and the catalog-lookup refactor — Phase 17.
- HTTPS/HSTS at the host layer — depends on the Phase 9 nginx work.

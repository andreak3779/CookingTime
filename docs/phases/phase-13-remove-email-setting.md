# Phase 13 — Remove personal contact data; fix client configuration delivery

| | |
| --- | --- |
| **Branch** | `phase/13-remove-email-setting` |
| **Cuts from** | `phase/12-ui-theme` (the tip of real work; `main` is its ancestor) |
| **Checkpoint** | `checkpoint(phase-13): remove personal contact data from config/UI + fix client config delivery + ADR 0008 + phase doc` |
| **Tag** | `v0.13-remove-email-setting` (after merge) |
| **Production code** | `wwwroot/appsettings.json` (**moved** from the repo root, `Email` key removed), `Infrastructure/Configuration/ProductInfoOptions.cs` (`Email` property removed), `Pages/About.razor` (`mailto:` block removed) |
| **Tests** | New `tests/CookingTime.UnitTests/ClientConfigurationDeliveryTests.cs` (4 facts); updated `tests/CookingTime.ComponentTests/AboutRazorTests.cs` (2 facts updated, 1 guard added); deleted `tests/CookingTime.UnitTests/Application/BinderProbe.cs` (a planning probe, never production). **Total: 156 facts** (was 152) |
| **Docs** | This file; new [ADR 0008](../decisions/0008-no-personal-contact-data.md); updated `docs/decisions/README.md`; new `docs/plan-remove-pii.md`; updated `docs/phases/README.md`; updated `docs/LINKEDIN.md`; updated `AGENTS.md`; updated `docs/phases/phase-0-docs-scaffold.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 156 facts green |

## Goal

Remove the owner's personal email address from everything the app and the
repository publish, and — while auditing how that address could have reached a
browser — fix the configuration-delivery defect the audit uncovered.

## What shipped

### 1. The address is gone from the app, end to end

`ProductInfo.Email` was a first-class configuration option, not just a JSON
line. It was threaded through four places, all now removed in one commit:

| Location | Change |
| --- | --- |
| `wwwroot/appsettings.json` | `"Email"` key deleted (and the trailing comma on the line above it) |
| `Infrastructure/Configuration/ProductInfoOptions.cs` | `public string Email { get; set; } = "";` deleted |
| `Pages/About.razor` | the conditional `mailto:` block deleted |
| `docs/design/cookingtime-theme/razor/Pages/About.razor` | the same block deleted — see below |

**Why the design package copy matters.** Per
[ADR 0007](../decisions/0007-design-package-as-reference-artifact.md) the
package under `docs/design/` is a *reference artifact*: it is not compiled
(`EnableDefaultRazorComponentItems=false`), but the app is updated by **copying
files out of it**. Leaving the block in the copy would mean the next "apply the
theme" step reintroduces it — and, because the property no longer exists, **the
build breaks**. It had to change in the same commit.

**Unknown keys are ignored.** The `IOptions<T>` binder silently ignores
configuration keys with no matching property, so a deployed `appsettings.json`
that still carries `ProductInfo:Email` keeps working — no startup exception.
The behavior changes (the link disappears), but nothing breaks.

### 2. The audit found a real functional bug: the browser received no configuration

The original plan asserted that the address "ships to every visitor", because a
Blazor WebAssembly client fetches `appsettings.json` at boot. **That was wrong
for this repository**, and proving it turned up a defect worth more than the
removal itself.

The Blazor WASM SDK bundles configuration only from `wwwroot/`:

```xml
<!-- Microsoft.NET.Sdk.BlazorWebAssembly.5_0.targets:132 -->
<_BlazorConfigFile Include="wwwroot\appsettings*.json" />
```

`CookingTime` kept `appsettings.json` at the **repository root**, and
`git log --diff-filter=A -- wwwroot/appsettings.json` shows it was **never**
in `wwwroot/`. Proven by publishing three probe apps and extracting the
embedded boot manifest from `dotnet.js`:

| Probe | `appsettings.json` location | Boot manifest `appsettings` key |
| --- | --- | --- |
| A/B | repo root **and** `wwwroot/` | `['../appsettings.json']` ✅ |
| Control | repo root only | **ABSENT** ❌ |
| CookingTime (before this phase) | repo root only | **ABSENT** ❌ |

So the browser got **no configuration at all**. `IOptions<ProductInfoOptions>`
fell back to the POCO defaults, and — more visibly —
`MealCatalogOptions.Catalog` was **empty**, so the production meal dropdown
offered only the two code-baked meals (Chicken, Turkey) and none of the ten
configured Range meals.

**No test caught it.** The build stays green (a misplaced config file is not a
compile error), the server returns HTTP 200 for every route (it serves the SPA
shell), bUnit injects a mocked `IMealFactory`, and the integration suite loads
the file **server-side** from the content root — never through the browser's
boot manifest. The failure only ever surfaces in a real browser.

**The fix.** `appsettings.json` moved to `wwwroot/`, where the SDK bundles it.
Verified by publishing and extracting the manifest: the `appsettings` key is
now `['../appsettings.json']`, and the file appears in the published `wwwroot/`.

**The tension this creates — and how it is resolved.** Once the file lives in
`wwwroot/`, the server **serves it to every visitor**. The removal in §1 is
therefore not cosmetic hygiene: it is what keeps a personal address from
becoming a live exposure the moment the configuration starts working. The two
changes had to land together, which is why they are in one phase. This is
recorded in [ADR 0008](../decisions/0008-no-personal-contact-data.md)
(decision 3), which also pins Phase 9's container document root to `wwwroot/`
— never the publish root, where `dotnet publish` copies the config file.

### 3. Regression guards

Two guards, because both failures are invisible to the build and to the
existing suite:

- `AboutRazorTests.ProductInfoOptions_HasNoEmailProperty` — fails if the
  `Email` property returns.
- `ClientConfigurationDeliveryTests` (4 facts) — asserts the config lives in
  `wwwroot/`, that no root-level copy exists, that the served file contains no
  email address, and that the project file adds no root-level config entry.

Both were **verified to have teeth**: moving `appsettings.json` back to the
repo root fails 3 of the 4 delivery guards, and re-adding the `Email` property
fails the property guard.

### 4. Docs and identity hygiene

- Personal local filesystem paths (`/home/<user>/...`) scrubbed from
  `AGENTS.md`, `docs/LINKEDIN.md`, and `docs/phases/phase-0-docs-scaffold.md`.
- `docs/LINKEDIN.md` claimed the source repo was **private**; it is **public**
  (`gh repo view` → `PUBLIC`, 0 forks, 8 merged PRs). Corrected.
- New [ADR 0008](../decisions/0008-no-personal-contact-data.md) records the
  standing policy, including the decision to **not** rewrite git history by
  default and the third-party address left verbatim in `OriginalSource/`.
- Commit identity for this phase uses GitHub's noreply form. The repository-level
  `git config` must be applied by the owner; where `.git/config` is not
  writable, the identity is applied per-invocation with `git -c user.email=…`.

### 5. A false positive, corrected

An earlier draft of the plan listed the CI-published coverage XML as a PII
surface, because a *local* run embeds `<source>/home/<user>/src/…</source>`.
That is **wrong**: on a GitHub runner the source root is
`/home/runner/work/<repo>/<repo>/` — confirmed against the real CI log for run
`36658844870`, which uploaded the `coverage-cobertura` artifact — which carries
no owner data. The local XMLs are gitignored and never published. **No fix was
needed, and none was made.** Recorded here because the plan document is the
audit trail, and an unretracted false positive in it would mislead the next
reader.

## Verification

```
dotnet build -c Release   →  0 Warning(s), 0 Error(s)
dotnet test  -c Release   →  UnitTests 112, ComponentTests 16, IntegrationTests 28 — all green
```

- Boot manifest after `dotnet publish`: `appsettings = ['../appsettings.json']`
  (was `ABSENT`).
- `grep -rn` for the address across the working tree → no tracked-file hits.
- The served `wwwroot/appsettings.json` contains no email address.
- Both guards confirmed to fail when their invariant is broken.

## Out of scope

- **Git history rewrite.** The address remains in published history since
  `d574e18` (2026-07-29). The verified `git-filter-repo` procedure is in
  `docs/plan-remove-pii.md` §7-B and runs only as its own approved task.
- **Third-party address** in `OriginalSource/frmAbout.cs` — left verbatim per
  the Phase 8 rule; acting on it means deleting the folder wholesale under a new
  ADR.
- **Git identity on existing commits** — only future commits are affected.

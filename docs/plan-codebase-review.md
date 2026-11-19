# CookingTime — Codebase Review & Remediation Plan

Date: 2026-11-19 · Baseline commit: `d32c850` · Target: `net10.0` Blazor WASM

Scope: full codebase (production + tests + CI + docs), not a diff.
Method: manual read of every production file, empirical probes against the real
compiled assemblies, live HTTP verification against the running app, plus an
independent OWASP Top 10 (2021) review.

> **Status: complete.** Every finding below was fixed in Phases 14–17. The
> dispositions are recorded in section 6 and in
> [ADR 0009](decisions/0009-accepted-security-risks.md). This document is
> kept as the **baseline** to re-run the review against a later commit and
> diff the result.

---

## 1. Architecture understanding

```
Domain/         no deps      value objects, strategies, meals, registry
Application/    -> Domain    ICookingTimeCalculator, CookingResult, DI wiring
Infrastructure/ -> Domain+App MealFactory, options POCOs
Layout/,Pages/  -> all three Razor UI (Blazor WASM client)
wwwroot/        static       index.html, js/theme.js, css, fonts, icons, help
tests/          UnitTests (112) | ComponentTests bUnit (16) | IntegrationTests (30)
```

Shape is genuinely good: no file over ~120 lines, no kitchen-sink files, no
`TODO`/`FIXME`, layering respected, SOLID mostly honoured (Strategy for
cooking algorithms, Factory+Registry for meal creation, `readonly record struct`
value objects, DI at the composition root only). 158 tests, 78.77 % gated
coverage, 0 warnings with `TreatWarningsAsErrors`.

The problems below are **not** structural. They are concentrated in the
**domain's input validation**, **config trust boundaries**, and
**build/release reproducibility**.

---

## 2. Findings

### Correctness (highest real-world impact)

| ID | Sev | Location | Problem |
|----|-----|----------|---------|
| C-01 | 🔴 Critical | `Domain/Meals/RangeMeal.cs:45` | `RangeMeal` never validates weight against its own `MinimumWeight`/`MaximumWeight`. All **10 config-driven meals** (the entire shipped catalog) accept any weight. At extremes the user sees an impossible range. `ChickenMeal` correctly rejects — the two meal types disagree. |
| C-02 | 🟠 High | `Domain/ValueObjects/CookingDuration.cs:19-25` | Constructor checks non-negativity but never `Maximum >= Minimum`, so an inverted range is representable and reaches `Format()`. |
| C-03 | 🟡 Medium | `Domain/Strategies/RangeCookingStrategy.cs:20-24` | Never validates `maxMinutesPerPound >= minMinutesPerPound`. Latent (shipped config is correct) but a bad config entry silently yields an inverted range. |
| C-04 | 🟡 Medium | `Pages/CookingTime.razor:113-119` | `Reset()` mutates `_model` in place. `EditForm`'s `EditContext` caches `DataAnnotationsValidator` messages, so **stale validation messages survive Reset**. Empirically confirmed. |
| C-05 | 🟡 Medium | `Application/Services/CookingTimeCalculator.cs:41` | Meal lookup is `CreateAll().FirstOrDefault(by name)`. `CreateAll()` rebuilds every meal on every call; duplicate names silently resolve to the first. |

### Security (OWASP Top 10)

| ID | OWASP | Sev | Location | Problem |
|----|-------|-----|----------|---------|
| S-01 | A03 | 🟠 High | `Pages/About.razor:18` | `ResumeUrl` is rendered into `href` with no scheme allowlist. Razor HTML-encoding does **not** neutralise `javascript:`. Mitigated today only by an accidental side effect of `target="_blank"` (independently verified: it does not execute on click). Do not rely on that. |
| S-02 | A05 | 🟡 Medium | `wwwroot/index.html:1-48` | No Content-Security-Policy. A WASM app needs `script-src 'wasm-unsafe-eval'`; `Pages/Index.razor` inline styles need `style-src 'unsafe-inline'`. |
| S-03 | A06/A08 | 🟡 Medium | `CookingTime.csproj:43-44`, `tests/…IntegrationTests.csproj:26-27` | Floating versions (`10.0.0-*`), no `packages.lock.json`, no `NuGet.config` source mapping. Builds are not byte-reproducible. |
| S-04 | A04 | 🟡 Medium | `Application/ServiceCollectionExtensions.cs:34-38` | Options bound with no `ValidateOnStart()` / `ValidateDataAnnotations()`. A malformed meal entry throws from the `MealFactory` constructor at **first render**, not at startup. |
| S-05 | A05 | ⚪ Low | `.github/workflows/ci.yml:29,32,37,76` | Actions pinned to mutable tags, not commit SHAs. `permissions: contents: read` is already minimal. |
| S-06 | A09 | ⚪ Low | whole production tree | No logging at all. Primarily an observability gap, not a vulnerability. |
| S-07 | A04 | ⚪ Low | `tests/…UnitTests/ClientConfigurationDeliveryTests.cs` | The appsettings secret guard only checks for `@`. An API key without `@` passes. |
| S-08 | A05 | ⚪ Low | `wwwroot/appsettings.json:1` | Leading `// filepath:` comment — not strict JSON. |

**Not applicable (documented as accepted risk):** A01 access control, A02
cryptography, A07 authentication, A10 SSRF. No server, no DB, no `HttpClient`,
no auth, no outbound fetch in production code.

**Verified clean — do not spend effort:** no `MarkupString`/`Html.Raw` anywhere
(no raw-HTML sink exists); all 4 legacy `wwwroot/help/*.htm` are static text
with no script/handlers; `theme.js` and `cookingtime.css` are safe;
DevServer is correctly excluded from publish output; the bUnit/AngleSharp CVE
suppression is correctly scoped to test projects only; `OriginalSource/` is not
compiled and not served (its third-party email is deliberate per ADR 0008 §7).

### Build / release / process

| ID | Sev | Location | Problem |
|----|-----|----------|---------|
| B-01 | 🟡 Medium | solution-wide `dotnet publish` | Publishing the **solution** copies all three test assemblies, xunit, Moq, bUnit and coverlet into the publish root. `dotnet publish CookingTime.csproj` is clean — the risk is only in the invocation. Phase 9 (containerise) will hit this. |
| B-02 | ⚪ Low | `tests/…/*.csproj` | Dependency version skew: bUnit `1.40.0` vs `1.31.3`; FluentAssertions `6.12.1` vs `6.12.2`. |
| B-03 | ⚪ Low | `README.md:9,11,23,27,105` | Stats stale: says 156 facts / 80.0 %; actual 158 / 78.77 %. |
| B-04 | ⚪ Low | `Domain/Meals/MealTypeRegistry.cs:36,69` | `TryCreate` and `RegisteredKinds` are public API used only by tests — dead production surface. |

---

## 3. Empirically verified evidence

**C-01 — the critical one.** Via the real application layer:

```
Pork Roast @ 0.5 Pounds   IsSuccess=True   Duration="Cooking time will be between of 0 hrs. and 20 mins. to 0 hrs. and 22 mins."
Pork Roast @ 500 Pounds    IsSuccess=True   Duration="Cooking time will be between of 21 hrs. and 20 mins. to 15 hrs. and 0 mins."
Chicken     @ 0.5 Pounds   IsSuccess=False  Errors=["Weight 0.5 lbs is outside the supported range for Chicken."]
```

The 500 lb case is the smoking gun: **minimum (21h20m) exceeds maximum (15h00m)**,
and the app reports it as a *success*. `ChickenMeal` — which delegates to
`TableLookupStrategy` and throws — rejects the same input. The inconsistency
is the bug.

**C-02.** `new CookingDuration(5h, 1h)` constructs successfully and formats as
"between of 5 hrs. and 0 mins. to 1 hrs. and 0 mins."

**C-03.** `new RangeCookingStrategy(45, 40).Calculate(2lb)` → "between of 1 hrs.
and 30 mins. to 1 hrs. and 20 mins."

**C-04.** bUnit probe: after a failed submit then clicking Reset —
`validation-message` elements before reset: `[Please select a meal. | Please
select a meal.]`; **after reset: unchanged**. Changing `Reset()` to assign a
fresh model instance clears them (`[]`). Probe reverted; not left in the tree.

**S-01.** `javascript:alert(1)` contains no characters Razor encodes, so it
passes through an `href` verbatim. Independently verified in Chromium 158 that
`target="_blank"` prevents execution on a real click — but an anchor without
`target="_blank"` executes it.

**B-01.** `dotnet publish -c Release` (solution) → publish root contains
`CookingTime.UnitTests.dll`, `CookingTime.ComponentTests.dll`,
`CookingTime.IntegrationTests.dll`, `xunit.*`, `Moq.dll`, `Bunit.*`,
`FluentAssertions.dll`, `coverlet.*`. `dotnet publish CookingTime.csproj` →
none of these.

---

## 4. Proposed remediation plan

Each step is a branch + phase doc + checkpoint commit, per ADR 0002.

### Phase 14 — Domain input validation (C-01, C-02, C-03)
The correctness core. Highest user impact.
1. `CookingDuration`: throw when `Maximum < Minimum`.
2. `RangeCookingStrategy`: validate `max >= min` in the constructor.
3. `RangeMeal.Calculate`: validate weight against `MinimumWeight`/`MaximumWeight`
   and throw `ArgumentOutOfRangeException` — matching `TableLookupStrategy`.
   This closes the 10-meal hole.
4. `CookingTimeCalculator`: catch it and surface the existing
   "outside the supported range" message.
5. Tests: unit tests for each guard; an application-level test asserting an
   out-of-range weight yields `IsSuccess == false` for a Range meal.
6. Note: `RangeCookingStrategy` has no knowledge of the meal's range, so the
   guard belongs in `RangeMeal` (which owns min/max), not in the strategy.

### Phase 15 — Config trust boundary (S-01, S-02, S-04, S-07, S-08)
7. `ProductInfoOptions`: add a `ResumeUrl` scheme allowlist
   (`http`/`https` only) — validated in the options POCO and applied at the
   render site in `About.razor`. Test the `javascript:` case explicitly.
8. `AddCookingTime`: `AddOptions<T>().Bind(...).ValidateDataAnnotations().ValidateOnStart()`.
9. A custom `IValidateOptions<MealCatalogOptions>` that rejects a catalog
   entry missing minutes-per-pound, or with `MinPounds > MaxPounds`, or
   duplicate names — failing fast at startup instead of at first render.
10. CSP meta tag in `wwwroot/index.html`:
    `default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'`.
11. Broaden the appsettings guard from "contains `@`" to a secret-pattern denylist.
12. Remove the `// filepath:` comment from `appsettings.json` (or move it to a
    sidecar) so the file is strict JSON.

### Phase 16 — Build & release hardening (S-03, S-05, B-01, B-02)
13. Pin exact package versions; add `packages.lock.json`
    (`RestorePackagesWithLockFile`); add a `NuGet.config` with package-source
    mapping.
14. Pin GitHub Actions to commit SHAs; add `.github/dependabot.yml`.
15. Add a CI step that fails on `dotnet list package --vulnerable
    --include-transitive` for the production project.
16. Pin `dotnet publish CookingTime.csproj` (not the solution) in the Phase 9
    container build, and add a CI assertion that no `*Tests*.dll` lands in the
    publish output.
17. Align the bUnit and FluentAssertions versions across test projects.
18. Add `global.json` pinning the SDK version.

### Phase 17 — UI robustness & cleanup (C-04, C-05, B-03, B-04)
19. `Reset()` assigns a fresh `CookingTimeViewModel`; add a bUnit test asserting
    the validation messages clear.
20. `IMealFactory` gains a name-keyed lookup so `CalculateAsync` stops
    rebuilding the whole catalog per call; reject duplicate names.
21. Update README stats.
22. Either use `TryCreate`/`RegisteredKinds` in production or remove them with
    their tests.
23. Consider `ILogger` in `CookingTimeCalculator`/`MealFactory` (S-06).

### Phase 18 — Accepted risks (ADR 0009)
24. Record as deliberate, not overlooked: no auth (A07), no access control
    (A01), no crypto (A02), no SSRF surface (A10), no raw-HTML sink.
25. Record that `OriginalSource/` retains a third-party email by design
    (ADR 0008 §7) and that `wwwroot/appsettings.json` is public by design and
    must never hold secrets.

---

## 5. Sequencing

```
Phase 14 (domain validation)   ← highest impact, no dependencies
Phase 15 (config trust)        ← independent of 14
Phase 16 (build/release)       ← independent; do before Phase 9 containerise
Phase 17 (UI + cleanup)        ← independent
Phase 18 (ADR)                 ← summarise 14-17
```

Phases 15, 16 and 17 can run in parallel. Phase 16 should land **before**
Phase 9 (containerisation) so the publish step never ships test assemblies.

---

## 6. Dispositions — how each finding was closed

| ID | Finding | Phase | How it was closed |
| --- | --- | --- | --- |
| C-01 | `RangeMeal` never validated the weight against its own range | 14 | `RangeMeal.Calculate` enforces `[MinimumWeight, MaximumWeight]` and throws `ArgumentOutOfRangeException`; the calculator already caught it, so no application-layer change was needed |
| C-02 | `CookingDuration` permitted `Maximum < Minimum` | 14 | The constructor rejects it — the invalid state is now unrepresentable |
| C-03 | `RangeCookingStrategy` permitted `max < min` | 14 | The constructor rejects it, naming the real cause (a bad catalog entry) |
| C-04 | `Reset()` left stale `EditForm` validation messages | 17 | `Reset()` assigns a **fresh** model instance; verified with a bUnit probe and pinned by a test |
| C-05 | Catalog rebuilt on every calculation; lookup ambiguous | 17 | `IMealFactory.TryGetByName` + a name index built once in `MealFactory`; first-registration-wins preserves the old `FirstOrDefault` behaviour |
| S-01 | `javascript:` URI reachable from config | 15 | http/https scheme allowlist in `ProductInfoOptions`; the link renders only when the value passes |
| S-02 | No Content-Security-Policy | 15 | A `<meta>` CSP with `wasm-unsafe-eval` and `unsafe-inline` (style-src only), verified in Chromium |
| S-03 | Floating package versions, no lock file | 16 | Exact versions + committed `packages.lock.json` for all four projects + `--locked-mode` restore |
| S-04 | No startup validation of the config | 15 | `IValidateOptions<T>` validators registered with `ValidateOnStart()`; the message names the offending entry by index |
| S-05 | GitHub Actions pinned to mutable tags | 16 | All four pinned to full commit SHAs (verified against their release tags) + dependabot |
| S-06 | No logging anywhere | 17 | `ILogger` in the calculator: unknown meal at `Warning`, out-of-range weight at `Debug` |
| S-07 | The served-config guard only checked for `@` | 15 | A 13-case theory over secret *shapes* |
| S-08 | `appsettings.json` was not strict JSON | 15 | The `// filepath:` comment removed; a test asserts the file parses strictly |
| S-09 | No CSP in `index.html` | 15 | Same as S-02 |
| F-12 | DevServer in the publish output | — | **Verified not shipped.** `PrivateAssets="all"` and absent from the published artifact. No action |
| F-14 | Third-party address in `OriginalSource/` | 18 | **Deliberate** — ADR 0008 §7 requires the folder to stay verbatim. Recorded in ADR 0009 §6 |
| F-15 | Stale README statistics | 14–17 | Updated in every phase; now 248 facts / 86.01 % |
| F-16 | Dead public API (`TryCreate`, `RegisteredKinds`) | 17 | Removed, with their five tests; replaced by one fact covering the same ground |
| B-01 | Test assemblies in the publish root | 16 | CI now publishes `CookingTime.csproj` explicitly and **fails** if a test assembly appears |
| B-02 | Test-project dependency skew | 16 | bUnit and FluentAssertions aligned across all three test projects |
| F-11, S-11 | No auth / no crypto / no SSRF | 18 | **Not applicable** — recorded as accepted decisions in ADR 0009 |

### Findings that were checked and found clean — do not re-investigate

- The 4 legacy `wwwroot/help/*.htm` files: no `<script>`, no `on*=` handlers.
- `wwwroot/js/theme.js`: `setAttribute` only, values constrained to literals.
- `wwwroot/css/cookingtime.css`: no `expression()`, no `url(javascript:)`, no external refs.
- No `MarkupString` / `Html.Raw` / `innerHTML` / `eval(` anywhere — no raw-HTML sink exists.
- No `HttpClient` / `fetch(` in production code — A10 SSRF and A02 crypto are genuinely N/A.
- The bUnit/AngleSharp CVE suppression is correctly scoped to the test projects only.
- `OriginalSource/` is neither compiled nor served.

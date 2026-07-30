# CookingTime — LinkedIn Project Summary

> Drop-in copy for a LinkedIn post, a featured-project card, and a resume bullet. The
> full repository lives at `/home/andreak/src/CookingTime`. The original WinForms code
> is preserved untouched under `OriginalSource/` for historical reference.

---

## One-line pitch

Refactored a legacy .NET Framework WinForms cooking-time calculator into a modern,
fully-tested **ASP.NET Core Blazor WebAssembly on .NET 10** application using clean
architecture, SOLID, a Strategy + Factory + Value-Object domain model, and a blocking
CI gate with line-coverage enforcement.

---

## Headline for the LinkedIn card

**CookingTime — .NET Framework → .NET 10 · Clean architecture · 0-warning build · 118 automated tests · ≥75 % coverage gate**

---

## Project at a glance

| | |
| --- | --- |
| **Type** | Personal / portfolio modernization project |
| **Source** | In-repo (`/home/andreak/src/CookingTime`), private |
| **Stack** | C# · .NET 10 · Blazor WebAssembly · xUnit · bUnit · Moq · FluentAssertions · coverlet · GitHub Actions · Python |
| **Legacy** | .NET Framework 2.0 WinForms (VS 2005 era, `msbuild` XML 2003 schema, `OutputType=WinExe`) |
| **Tests** | 118 facts across unit, Razor component, and HTTP/integration tiers — all green |
| **Coverage** | 80 % line on `CookingTime.UnitTests` production code; CI gate at 75 % |
| **Build** | `dotnet build -c Release` — **0 warnings**, 0 errors (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`) |
| **AI toolchain** | AI coding agent used end-to-end under disciplined branch / checkpoint / ADR workflow |

---

## Improvements shipped during the migration

### 1. Platform & framework

- Migrated from **.NET Framework 2.0 WinForms** (Windows-only desktop) to **.NET 10 Blazor WebAssembly** (cross-platform, modern SPA).
- Replaced the legacy MSBuild XML (`<OutputType>WinExe</OutputType>`, .NET 2.0 schema) with an SDK-style csproj and a `.slnx` solution.
- Enabled nullable reference types, implicit usings, latest C# language version, and **warnings-as-errors** so every green build is a real green build.
- Switched the data source from a hand-edited `meal.xml` to a typed, validated `appsettings.json` bound through `IOptions<T>`.
- Removed the global `Resources`, `Properties`, `*.resx`, and `App.ico` baggage; kept `OriginalSource/` as historical reference only.

### 2. Architecture — from kitchen-sink to layered

The original codebase had ~700 lines across one file mixing domain, infrastructure, and UI-adjacent code. After:

```
Domain/         → no project dependencies
Application/    → references Domain only
Infrastructure/ → references Domain + Application
Components/     → references all three (Razor UI)
wwwroot/        → static assets
tests/          → unit / component / integration
```

Encapsulated by a **single composition root** (`Application/ServiceCollectionExtensions.AddCookingTime`) consumed by both the WASM client and the integration test host — no parallel wiring to drift.

### 3. SOLID + design patterns — applied, not decorative

| Principle / Pattern | Where it lives now |
| --- | --- |
| **S**ingle responsibility | Each value object, strategy, meal, factory, service has one reason to change. No more 700-line files. |
| **O**pen / closed | `MealTypeRegistry` registers kinds via `Register(MealKind, Func<IMeal>)`. New meals don't edit a switch. |
| **L**iskov | No `null`-returning virtuals; strategies validate non-empty rows up front. |
| **I**nterface segregation | `ICookingStrategy`, `IMeal`, `IMealFactory`, `ICookingTimeCalculator` — four small, role-specific contracts. |
| **D**ependency inversion | UI depends on `ICookingTimeCalculator`; DI wires `CookingTimeCalculator`. WinForms `new CT.MealCreator()` is gone. |
| **Strategy** | `RangeCookingStrategy` (linear `weight × minutes/lb`) and `TableLookupStrategy` (ordered row lookup). |
| **Factory + Registry** | `IMealFactory` + `MealTypeRegistry`; `MealFactory.CreateAll()` materializes the catalog from config. |
| **Value Object** | `Weight` and `CookingDuration` are `readonly record struct` — immutable by construction. |
| **Result-with-validation** | `CookingResult` DTO; calculator never throws for expected validation failures. |
| **Adapter** | UI weight units (lb / kg) → domain `Weight` value object via one conversion point. |

### 4. Real, reproducible bug fixes (legacy → new)

| # | Legacy bug | Where it lived | Fix in the new code |
| --- | --- | --- | --- |
| 1 | **Mutable value object**: the `MinimumTime` setter wrote to `m_MaxTime` — silent corruption in `MealBasic` / `MealRange`. | `OriginalSource/ComCookingTime.cs:360` | `CookingDuration` is a `readonly record struct` — the entire class of bug is structurally impossible. |
| 2 | **Wrong unit-conversion direction**: `structWeight.Weight` multiplied entered kilograms by `2.205` instead of dividing — kg input produced a value 5× too large. | `OriginalSource/ComCookingTime.cs:421` | Single conversion point in `Weight.FromKilograms` using the correct factor `2.20462262M`; round-trip test verifies to 4 decimal places. |
| 3 | **Both `"Beef Roast Standing Rib — Rare"` entries** were labeled `"Rare"` in `MealCreator.GetMeals()` — the second one was actually Medium. | `OriginalSource/ComCookingTime.cs:500-503` | `appsettings.json` carries the corrected labels; the typo cannot recur. |
| 4 | **Half-built Range meals**: three entries passed only `aMin`, leaving `aMax=0` and producing nonsense durations. | `OriginalSource/ComCookingTime.cs:529+` | Each Range entry now explicitly sets both min and max minutes-per-pound. |
| 5 | **Concrete dependencies in UI**: `frmCookingTime` instantiated `new CT.MealCreator()` and `new CT.structWeight(...)`. | `OriginalSource/frmCookingTime.cs:69` | UI depends on `ICookingTimeCalculator`; DI wires the concretes via the shared extension. |
| 6 | **Switch-on-string factory**: adding a meal meant editing `MealCreator.CreateMeal`. | `OriginalSource/ComCookingTime.cs:444` | `MealTypeRegistry.Register` — new meals are additive (OCP). |

### 5. Tests — written with the code, not after

Three test projects, one source of truth for naming (`<Method>_<Scenario>_<Expected>`):

| Project | What it covers | Stack |
| --- | --- | --- |
| `CookingTime.UnitTests` | Domain, Application, Infrastructure (canonical coverage surface — drives the CI gate) | xUnit · Moq · FluentAssertions · coverlet |
| `CookingTime.ComponentTests` | Razor components rendered with real DI | + bUnit 1.31 |
| `CookingTime.IntegrationTests` | End-to-end via `WebApplicationFactory<Marker>` + in-process TestServer | + `Microsoft.AspNetCore.Mvc.Testing` |

Totals at the end of Phase 7: **118 facts, 0 failed, 0 skipped**.

Highlights:

- Every kilogram / pound round-trip is verified to 4 decimal places.
- Every one of the 8 turkey cooking-table rows is exercised at 3 boundary weights (24 cases), plus 4 out-of-range cases.
- Every guard branch in `CookingTimeCalculator` is independently tested (validation errors, kg / lb conversion failure, meal rejection, unit-enum fallback).
- HTTP route smoke tests prove the SPA fallback, static `wwwroot/help/*` files, and the Blazor shell.
- Feature tests run the production `Pages/CookingTime.razor` form through the same DI composition root the WASM client uses.

### 6. CI / quality gate

`.github/workflows/ci.yml` is the **single source of truth** for whether a PR is mergeable:

1. `dotnet restore`
2. `dotnet build -c Release --no-restore` — fails on any warning.
3. `dotnet test --settings coverlet.runsettings.xml` per test project — collects Cobertura XML.
4. `python3 scripts/check-coverage.py … --threshold 75` — fails the build if `CookingTime.UnitTests` drops below 75 % line coverage on production code.
5. Uploads the three Cobertura XMLs as the `coverage-cobertura` artifact (14-day retention).

Triggers: `pull_request` to `upgrading-to-net-10`, `push` to `upgrading-to-net-10` and every `phase/*` branch.

Measured baseline: UnitTests = **80.00 %** (gate 75 %), ComponentTests = 15.00 %, IntegrationTests = 63.07 % (both informational).

### 7. Documentation as code (ADRs + phase docs)

Every meaningful decision is recorded where future readers look:

- **`docs/plan.md`** — locked master plan.
- **`docs/phases/phase-N-*.md`** — eight phase summaries (docs → host → domain → app+config → UI → integration → cleanup → CI). Each one lists the verification numbers.
- **`docs/decisions/000N-*.md`** — append-only ADRs (target framework, branch model, docs-as-code, SOLID expectations, test-host naming, coverage tooling).
- **`docs/onboarding.md`** — 30-minute newcomer orientation with a code-reading order.
- **`AGENTS.md`** — standing rules for any AI agent (or human) entering the repo; mirrors `ADR 0004` with project-specific conventions.

---

## Technical skills demonstrated

**Languages & frameworks**
C# 12/13, .NET 10 (ASP.NET Core, Blazor WebAssembly), .NET Framework 2.0 (legacy target)

**Architecture & patterns**
Clean architecture (Domain / Application / Infrastructure / UI), SOLID, Strategy, Factory + Registry, Value Object, Result-with-validation, Adapter, Composition Root

**Testing**
xUnit 2.9, Moq 4.20, FluentAssertions 6.12, bUnit 1.31, `WebApplicationFactory<TEntryPoint>`, coverlet 6.0.2, Cobertura XML parsing, custom Python coverage gate

**DevOps & tooling**
GitHub Actions (`ubuntu-latest`, .NET 10 setup), NuGet caching, artifact upload, NuGet CVE handling (`NU1902`, `NU1903`), `TreatWarningsAsErrors`, `dotnet format`-friendly style

**Tooling / IDE**
VS Code, C# Dev Kit, .NET SDK 10.0.x, MSBuild (`Directory.Build.props`, `<InternalsVisibleTo>`, sdk-style csproj, .slnx)

**Practices**
TDD-flavored test-writing alongside code, branch-per-phase + checkpoint commits, append-only ADRs, docs-as-code, layered enforcement (CI gate + ADRs + AGENTS.md working together)

**AI-assisted coding**
End-to-end migration executed with a coding agent under a disciplined `branch → checkpoint → ADR → AGENTS.md` workflow. The agent had to read the project before every change (`AGENTS.md`), respect layering, and ship per-phase docs.

---

## AI tools & models used

> The migration was driven by **AI-assisted coding**. The interesting part is not "an AI wrote it" but the workflow that kept a working green build across 9 phases.

- **Agent environment:** local coding agent following a `phase-execution` skill seeded with `docs/plan.md`.
- **Model:** GitHub Copilot (the assistant you talk to in VS Code), powered by `minimax-m3:cloud`.
- **Workflow:** `git checkout -b phase/N-…` → execute phase → commit `checkpoint(phase-N): …` → tag `v0.N-…` → write `docs/phases/phase-N-*.md` → CI must be green → merge.
- **Ground-truth files the agent had to honour on every task:** `AGENTS.md`, `docs/plan.md`, `docs/decisions/0004-solid-and-patterns.md`, the in-phase doc, the existing tests.
- **Skills invoked:** `phase-execution` (drives one phase against the plan), `modernize-legacy-dotnet` (modernization patterns), plus ad-hoc reads of the ADRs to recover decision context.

This is the **demonstrable, repeatable recipe** for using a coding agent on a non-trivial .NET codebase without producing tech debt: keep the plan and the rules in the repo, not in the agent's hidden memory.

---

## Quick-start commands (so a reader can run it)

```bash
dotnet restore
dotnet build -c Release                 # 0 warnings, 0 errors
dotnet test --settings coverlet.runsettings.xml
# Optional: run the coverage gate locally
python3 scripts/check-coverage.py \
  "$(ls -t tests/CookingTime.UnitTests/TestResults/*/coverage.cobertura.xml | head -1)" \
  --threshold 75
dotnet run                              # boots the Blazor app
```

---

## Suggested resume bullet

> Migrated a .NET Framework WinForms calculator to ASP.NET Core Blazor WebAssembly on .NET 10 across 9 disciplined phases: clean architecture (Domain / Application / Infrastructure / UI), SOLID + Strategy + Factory + Value-Object patterns, immutable `readonly record struct` value objects fixing two legacy VO bugs, 118 xUnit / bUnit / `WebApplicationFactory` tests shipping with code, a GitHub Actions CI gate with coverlet + Python coverage enforcement (≥ 75 % on production code), and full docs-as-code (master plan, append-only ADRs, per-phase docs, agent standing-rules). Result: 0-warning build, 80 % line coverage.

---

## Suggested LinkedIn post copy

> I've been modernizing a 20-year-old cooking-time calculator in my spare time — and the interesting part wasn't the .NET, it was the workflow.
>
> The legacy app was a single ~700-line `ComCookingTime.cs` WinForms file. It had a mutable value object whose setter wrote to the wrong field, a unit-conversion step that inverted kg → lb, and a `MealCreator` that required editing a string-based switch every time someone wanted to add a meal.
>
> The new version is Blazor WebAssembly on .NET 10, split into Domain / Application / Infrastructure / UI with a single composition root, immutable `readonly record struct` value objects, `Strategy + Factory + Registry` for meal kinds, and a result-with-validation DTO that never throws for expected input problems.
>
> 118 automated tests (xUnit + Moq + FluentAssertions for the domain, bUnit for Razor, `WebApplicationFactory<Marker>` for end-to-end). A GitHub Actions gate that blocks PRs below 75 % line coverage on production code. Eight append-only ADRs and a `docs/onboarding.md` that lets a new contributor (or a future AI agent) be productive in 30 minutes.
>
> Bonus: the migration was driven end-to-end by an AI coding agent working inside a `branch → checkpoint → ADR → AGENTS.md` loop. The plan, the constraints, and the per-phase deliverables all live in the repo — not in the agent's hidden memory. That's what made it reproducible instead of magic.
>
> Repo: github.com/andreak3779/CookingTime  ·  `#dotnet10` `#blazor` `#cleanArchitecture` `#SOLID` `#softwareArchitecture` `#testing` `#coverlet` `#githubActions` `#AIAssistedCoding` `#refactoring`

---

## Visual assets for the post

Drop one of these PNGs into the LinkedIn article (1:1 or 1.91:1) so the post has a hero image:

| Pick | File | Why this one |
| --- | --- | --- |
| **#1 (recommended)** | [`docs/diagrams/05-ai-workflow.png`](diagrams/05-ai-workflow.png) | Visualizes the AGENTS.md + branch + checkpoint loop — the differentiator |
| Architecture depth | [`docs/diagrams/02-domain.png`](diagrams/02-domain.png) | Domain abstractions + strategies + OCP registry — best "how it's built" image |
| Code walk | [`docs/diagrams/03-sequence.png`](diagrams/03-sequence.png) | Click Calculate → result rendered — best "show me it works" image |
| Process story | [`docs/diagrams/04-branches.png`](diagrams/04-branches.png) | 8-phase gitGraph — best "how the migration was shipped" image |
| Project card | [`docs/diagrams/01-architecture.png`](diagrams/01-architecture.png) | Layered architecture — matches the GitHub repo's "Architecture — at a glance" diagram |

For carousel posts use `01-architecture` → `03-sequence` → `05-ai-workflow` in that order — it's the "what → how → who's involved" arc.

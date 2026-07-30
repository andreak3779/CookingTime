# Onboarding — A new developer's guide to CookingTime

> Read this on day one. It points at the right docs and code, in the order a
> new developer actually needs them. It is **not** the source of truth — that
> lives in `docs/plan.md`, `docs/phases/`, and `docs/decisions/`. When something
> here disagrees with those, the others win.

## What this app does

A cooking-time calculator. The user picks a meal (e.g. "Whole Chicken",
"Beef Roast Standing Rib — Rare"), enters a weight and a unit (lb or kg), and
the app returns a cooking-time range plus cooking instructions. It is a single
page of business logic wrapped in a thin Blazor UI.

That is the entire product surface. The interesting part is **how** it is
structured — clean architecture, SOLID, and tests in every phase.

## 30-minute orientation

Read these, in order. Skim before you skim the code.

| # | Doc | Why |
| --- | --- | --- |
| 1 | [README.md](../README.md) | Status table, quick-start commands, where the legacy code lives. |
| 2 | [plan.md](plan.md) | The locked master plan: stack, branch model, architecture diagram. |
| 2b | [DIAGRAMS.md](DIAGRAMS.md) | Same layering block and the rest of the diagrams, rendered. Worth opening next to plan.md on a wide screen. |
| 3 | [phases/README.md](phases/README.md) | One row per shipped phase. Tells you what already exists so you don't re-implement it. |
| 4 | [decisions/README.md](decisions/README.md) | ADRs. Read **0001–0004** before touching anything; **0005–0006** before touching tests or CI. |
| 5 | [AGENTS.md](../AGENTS.md) | Standing rules for AI agents — but the SOLID + layering table applies to humans too. |

After that, jump to the code. The reading order below is the one that makes
the codebase make sense fastest.

## The architecture in one diagram

```
Domain/         → no project deps        — value objects, abstractions, meals, strategies
Application/    → refs Domain            — calculator service, DTOs, DI extensions
Infrastructure/ → refs Domain + App      — MealFactory, options POCOs
Components/     → refs all three         — Razor pages, layout, App.razor
wwwroot/        → static assets          — HTML, CSS, help files
tests/          → UnitTests, ComponentTests (bUnit), IntegrationTests (WebApplicationFactory)
OriginalSource/ → historical WinForms    — reference only; do not reference from new code
```

A class in `Domain/` must never `using` from `Application/` or
`Infrastructure/`. UI must not construct concretes — DI wires them. These two
rules are the spine of the codebase. See [ADR 0004](decisions/0004-solid-and-patterns.md)
for the full SOLID table.

> **Visual:** the same layering diagram rendered as a color PNG lives at
> [docs/diagrams/01-architecture.png](diagrams/01-architecture.png) and is
> embedded in the repo-root [README](../README.md). Keep that PNG open
> alongside this file when you're reading the code — it's a faster mental map
> than the ASCII block.

## Code reading order (≈ 90 minutes)

Open files in this order. Each one prepares you for the next.

### 1. Value objects (`Domain/ValueObjects/`)

These are the simplest things in the codebase and the foundation of
everything else.

- [Domain/ValueObjects/Weight.cs](../Domain/ValueObjects/Weight.cs) —
  `readonly record struct`, canonical pounds. Construct via `FromPounds` or
  `FromKilograms`. The kg→lb factor (`2.20462262`) lives here and **nowhere
  else**. Throws on non-positive input.
- [Domain/ValueObjects/CookingDuration.cs](../Domain/ValueObjects/CookingDuration.cs) —
  `(TimeSpan Minimum, TimeSpan Maximum)`. `Format()` reproduces the legacy
  output string verbatim so we can regression-test it exactly.

Why this matters: legacy `clsTime.MinimumTime` setter wrote to `m_MaxTime` —
the entire mutable-value-object class of bugs is **structurally impossible**
here.

### 2. Domain abstractions (`Domain/Abstractions/`)

- [IMeal.cs](../Domain/Abstractions/IMeal.cs) — what a meal exposes: name,
  instructions, kind, weight range, `Calculate(Weight)`.
- [ICookingStrategy.cs](../Domain/Abstractions/ICookingStrategy.cs) — the
  pluggable algorithm contract; throws `ArgumentOutOfRangeException` for
  out-of-range weights.
- [IMealFactory.cs](../Domain/Abstractions/IMealFactory.cs) — produces the
  catalog of meals.
- [MealKind.cs](../Domain/Abstractions/MealKind.cs) — `Chicken`, `Turkey`,
  `Range`. Compile-time enum, not a string.

### 3. Strategies (`Domain/Strategies/`)

- [RangeCookingStrategy.cs](../Domain/Strategies/RangeCookingStrategy.cs) —
  linear `weight × minutes-per-pound`. Single value when only `Min` is given.
- [TableLookupStrategy.cs](../Domain/Strategies/TableLookupStrategy.cs) —
  ordered rows of `(minLb, maxLb, minTime, maxTime)`. Throws if no row
  matches.

### 4. Concrete meals + registry (`Domain/Meals/`)

- [ChickenMeal.cs](../Domain/Meals/ChickenMeal.cs), [TurkeyMeal.cs](../Domain/Meals/TurkeyMeal.cs) —
  table-lookup meals, weights and rows from the legacy `ComCookingTime.cs`.
- [RangeMeal.cs](../Domain/Meals/RangeMeal.cs) — config-driven minutes-per-pound meal.
- [MealTypeRegistry.cs](../Domain/Meals/MealTypeRegistry.cs) — **OCP in
  action**. New meal kinds register, they never edit a switch.

### 5. Application service (`Application/`)

- [Abstractions/ICookingTimeCalculator.cs](../Application/Abstractions/ICookingTimeCalculator.cs) —
  the UI-facing contract. Async, accepts a meal name, decimal weight, and
  `WeightUnit`.
- [Common/CookingResult.cs](../Application/Common/CookingResult.cs) — the
  success/failure DTO. UI-friendly, never throws for expected validation
  failures.
- [Services/CookingTimeCalculator.cs](../Application/Services/CookingTimeCalculator.cs) —
  wraps the domain. Validates input, builds a `Weight`, calls the meal,
  catches domain `ArgumentOutOfRangeException`s, surfaces them as
  `CookingResult.ValidationErrors`.
- [ServiceCollectionExtensions.cs](../Application/ServiceCollectionExtensions.cs) —
  **the single composition root**. Both `Program.cs` and the Phase 5
  integration test host call `AddCookingTime(...)`. Anything new goes here,
  not in either entry point.

### 6. Infrastructure (`Infrastructure/`)

- [Configuration/MealCatalogOptions.cs](../Infrastructure/Configuration/MealCatalogOptions.cs) +
  [appsettings.json](../appsettings.json) — the meal catalog POCO and the
  config that drives `Range` meals (13 entries).
- [Configuration/ProductInfoOptions.cs](../Infrastructure/Configuration/ProductInfoOptions.cs) —
  the "About" page data.
- [Meals/MealFactory.cs](../Infrastructure/Meals/MealFactory.cs) — the real
  factory. Builds the registry from options; Chicken/Turkey are always
  registered from code (canonical domain knowledge), the rest come from
  config.

### 7. UI (`Program.cs`, `App.razor`, `Pages/`)

- [Program.cs](../Program.cs) — six lines; calls `AddCookingTime(builder.Configuration)`.
- [App.razor](../App.razor), [Routes.razor](../Routes.razor) — standard Blazor host.
- [Pages/CookingTime.razor](../Pages/CookingTime.razor) — the form:
  `EditForm` + `DataAnnotationsValidator` + `InputSelect` (meal),
  `InputNumber` (weight), `InputRadioGroup` (lb/kg), result panel below.
- [Pages/About.razor](../Pages/About.razor), [Pages/Help.razor](../Pages/Help.razor),
  [Pages/Index.razor](../Pages/Index.razor) — the rest of the surface.

### 8. Tests (`tests/`)

| Project | Frameworks | Targets |
| --- | --- | --- |
| `CookingTime.UnitTests` | xUnit + Moq + FluentAssertions | Domain, Application, Infrastructure. Coverage gate runs here (≥ 75% line on production code; see [ADR 0006](decisions/0006-coverage-tooling.md)). |
| `CookingTime.ComponentTests` | + bUnit | Razor components. |
| `CookingTime.IntegrationTests` | + `WebApplicationFactory` | End-to-end via real DI container. |

Naming convention: `<Method>_<Scenario>_<Expected>` (e.g.
`CalculateAsync_OutOfRangeWeight_ReturnsFailure`). Tests ship with code, not
after.

## How a request flows end-to-end

1. User picks a meal, weight, and unit in
   [Pages/CookingTime.razor](../Pages/CookingTime.razor), clicks **Calculate**.
2. The page calls `ICookingTimeCalculator.CalculateAsync(...)`.
3. [CookingTimeCalculator](../Application/Services/CookingTimeCalculator.cs)
   validates the input, builds a `Weight` value object, and looks up the
   `IMeal` via `IMealFactory.CreateAll()`.
4. The meal delegates to its `ICookingStrategy` and returns a `CookingDuration`.
5. The calculator formats via `CookingDuration.Format()` and returns a
   `CookingResult` (success or `ValidationErrors`).
6. The page renders the formatted duration and instructions.

You can trace this yourself in five minutes and you'll have the whole mental
model.

## Conventions you must respect

These are non-negotiable. See [AGENTS.md](../AGENTS.md) and
[ADR 0004](decisions/0004-solid-and-patterns.md) for the full list; the
non-negotiable subset:

- **Layering** — Domain has zero project deps; UI never news up concretes.
- **OCP for meals** — new kinds register in `MealTypeRegistry`; never switch
  on `MealKind` to construct.
- **C# defaults** — `<Nullable>enable</Nullable>`, file-scoped namespaces,
  `readonly record struct` for value objects, `sealed` on classes not
  designed for inheritance, `nameof()` over string literals.
- **Tests ship with code.** xUnit + Moq + FluentAssertions; bUnit for
  components; `WebApplicationFactory` for integration. Naming:
  `<Method>_<Scenario>_<Expected>`.
- **Coverage gate** — `CookingTime.UnitTests` must keep line coverage on
  production code ≥ 75% (see [ADR 0006](decisions/0006-coverage-tooling.md)
  and [phase-7-ci.md](phases/phase-7-ci.md)). Locally:

  ```bash
  python3 scripts/check-coverage.py \
    "$(ls -t tests/CookingTime.UnitTests/TestResults/*/coverage.cobertura.xml | head -1)" \
    --threshold 75
  ```
- **Branch model** — one feature branch per phase, checkpoint commit at the
  end (see [ADR 0002](decisions/0002-branch-per-phase.md)).
- **Docs as code** — per-phase docs evolve; ADRs are append-only; supersede
  with a new entry, never overwrite (see [ADR 0003](decisions/0003-docs-as-code.md)).

## Anti-patterns (do NOT reintroduce)

| Pattern | What it looked like | Why we moved away |
| --- | --- | --- |
| Mutable value object | `OriginalSource/clsTime.MinimumTime` setter writing to `m_MaxTime` | Silent bugs in MealBasic/MealRange. |
| Switch-on-string in factories | `OriginalSource/MealCreator.CreateMeal` if/else chain | Adding a meal required editing the factory. |
| Concrete deps in UI | `frmCookingTime.m_MealCreator = new CT.MealCreator()` | Untestable. |
| Domain + infrastructure + UI in one file | `OriginalSource/ComCookingTime.cs` (~700 lines, 4 concerns) | Impossible to reason about. |
| `using` legacy `OriginalSource/` types | future risk | Backward dependency; defeats the upgrade. |

If you're tempted by any of these, the answer is no.

## Local commands

```bash
dotnet restore
dotnet build -c Release                  # warnings-as-errors is on; treat warnings as bugs
dotnet test --settings coverlet.runsettings.xml
dotnet run                               # boots the Blazor app
```

`dotnet build` is the gate. PRs into `upgrading-to-net-10` and pushes to
`phase/*` must pass the workflow in `.github/workflows/ci.yml` (build + test +
coverage ≥ 75%). See [phase-7-ci.md](phases/phase-7-ci.md).

## How to make your first change

Pick the smallest thing that exercises the layering end-to-end. A new **Range
meal** is a good first PR because it touches every layer.

1. Add an entry to [`appsettings.json`](../appsettings.json) under `Meals.Catalog`.
2. (Optional) Read [Infrastructure/Meals/MealFactory.cs](../Infrastructure/Meals/MealFactory.cs)
   to confirm your entry matches the `Range` contract.
3. Add a unit test in
   `tests/CookingTime.UnitTests/Infrastructure/MealFactoryTests.cs` covering
   your new entry.
4. Run `dotnet build -c Release && dotnet test --settings coverlet.runsettings.xml`.
5. Run `dotnet run` and verify the new meal appears in the dropdown and
   computes a sensible time.

If you want to add a **table-lookup meal** (different algorithm), you need a
new `MealKind` and a new concrete meal class — that's a larger change, worth
its own phase branch. See the "Decisions worth recording" section of
[phase-2-domain.md](phases/phase-2-domain.md) for why.

## Where to get help

- A question about **what** the app does → [plan.md](plan.md) or the per-phase doc for the relevant area.
- A question about **why** it's built this way → the ADRs in [decisions/](decisions/README.md).
- A question about **how** to add a feature → the conventions in [AGENTS.md](../AGENTS.md) + this doc.
- A bug or surprise → check the tests in `tests/`. They are the executable spec.

Welcome — and remember the two rules: respect the layering, register don't switch.

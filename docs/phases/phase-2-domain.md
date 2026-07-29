# Phase 2 — Domain layer

| | |
| --- | --- |
| **Branch** | `phase/2-domain-layer` |
| **Cuts from** | `upgrading-to-net-10` (Phase 1 merged) |
| **Checkpoint** | `checkpoint(phase-2): domain layer + unit tests + ci smoke + phase doc` |
| **Tag** | `v0.2-domain` (after merge into `upgrading-to-net-10`) |
| **Production code** | `Domain/ValueObjects/{Weight, CookingDuration}.cs`, `Domain/Abstractions/{IMeal, ICookingStrategy, IMealFactory, MealKind}.cs`, `Domain/Strategies/{RangeCookingStrategy, TableLookupStrategy}.cs`, `Domain/Meals/{ChickenMeal, TurkeyMeal, RangeMeal, MealTypeRegistry}.cs`, `Infrastructure/Meals/MealFactoryStub.cs` |
| **Tests** | `tests/CookingTime.UnitTests/Domain/...` — 8 new test files, ~70 facts |
| **CI** | `.github/workflows/ci.yml` — first smoke workflow (build + test on PR open into `upgrading-to-net-10` and on push to `phase/*`) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Goal

Introduce the application domain — value objects, abstractions, strategies, concrete meals, and the factory registry — without yet wiring the UI or any configuration. Every layer carries its own tests.

## What shipped

### 1. Value objects (`Domain/ValueObjects/`)

| Type | Behavior | Notes |
| --- | --- | --- |
| `Weight` (`readonly record struct`) | Canonical pounds. Constructed only via `FromPounds` or `FromKilograms`. Both throw on non-positive input. `ToKilograms()` inverts `FromKilograms`. | The kg→lb conversion factor (`2.20462262`) lives here and **nowhere else**. Fixes the legacy `Backup/structWeight` bug where the conversion direction was inverted. |
| `CookingDuration` (`readonly record struct`) | `(TimeSpan Minimum, TimeSpan Maximum)`. Static `Single(time)` for a point value; constructor for ranges. `Format()` reproduces the legacy `clsTime.ToString()` output verbatim. | Immutable by record-struct semantics — the legacy `clsTime.MinimumTime` setter that wrote to `m_MaxTime` is structurally impossible here. |

### 2. Abstractions (`Domain/Abstractions/`)

| Type | Purpose |
| --- | --- |
| `MealKind` (enum) | `Chicken = 1, Turkey = 2, Range = 3`. Compile-time classification. |
| `IMeal` | `Name`, `Instructions`, `Kind`, `MinimumWeight`, `MaximumWeight`, `Calculate(Weight)`. |
| `ICookingStrategy` | `CookingDuration Calculate(Weight weight)`. Throws `ArgumentOutOfRangeException` for out-of-range weights. |
| `IMealFactory` | `IReadOnlyList<IMeal> CreateAll()`. Phase 2 implementation (`MealFactoryStub`) returns empty; Phase 3 wires `appsettings.json`. |

### 3. Strategies (`Domain/Strategies/`)

| Type | Behavior |
| --- | --- |
| `RangeCookingStrategy` | Linear `weight * timePerLb`. Constructor `(minPerLb, maxPerLb?)`; when `maxPerLb` is omitted, `Min == Max` and the duration collapses to a single value. Rounds to nearest minute. |
| `TableLookupStrategy` | Ordered rows of `(minPounds, maxPounds, min, max)`. Throws if no row matches. Constructor validates non-empty rows. |

### 4. Concrete meals (`Domain/Meals/`)

| Type | Strategy | Notes |
| --- | --- | --- |
| `ChickenMeal` | `TableLookupStrategy` with the 4 rows from `Backup/ComCookingTime.cs:MealChicken`. | Min 1.5 lb, max 6 lb. |
| `TurkeyMeal` | `TableLookupStrategy` with the 8 rows from `Backup/ComCookingTime.cs:MealTurkey`. | Min 6 lb, max 24 lb. |
| `RangeMeal` | `RangeCookingStrategy`. | Used for the beef/pork/ham entries in Phase 3. Validates `minPounds`, `maxPounds`, non-blank `name`. |
| `MealTypeRegistry` | `Dictionary<MealKind, Func<IMeal>>` populated via `Register(MealKind, Func<IMeal>)`. `TryCreate` returns false for unknown kinds; throws `InvalidOperationException` if a factory returns null. | OCP: new kinds register, they don't edit a switch. Phase 3 will add a config-driven overload. |

### 5. Infrastructure stub (`Infrastructure/Meals/MealFactoryStub.cs`)

Phase 2 placeholder that returns `Array.Empty<IMeal>()`. Real config-driven factory arrives in Phase 3.

### 6. Tests (`tests/CookingTime.UnitTests/Domain/`)

73 new facts covering:

- **`WeightTests`** — `FromPounds` round-trip, kg→lb conversion (factor verified to 4 decimal places), `ToKilograms` inversion, negative/zero input rejection (Theory with 2 cases), record-struct equality.
- **`CookingDurationFormatTests`** — single-hour, hours-and-minutes, range formatting (legacy string exact match), negative min/max rejection.
- **`RangeCookingStrategyTests`** — single value, range, fractional weight rounding, zero/negative inputs rejected.
- **`TableLookupStrategyTests`** — boundary inclusions (1.5, 2.5, 2.5001, etc.), out-of-range throws (Theory with 4 cases), empty-rows constructor throws.
- **`TurkeyTableStrategyTests`** — every one of the 8 turkey rows verified with 3 boundary weights each (24 cases), plus 4 out-of-range cases.
- **`ChickenMealTests`** — Name, Instructions, MinWeight, MaxWeight, two-pound happy path, out-of-range throws.
- **`RangeMealTests`** — constructor validation (blank name, negative range, max < min), happy-path math.
- **`MealTypeRegistryTests`** — register + TryCreate, unknown kind, null-factory rejection, null-instance rejection, registered kinds enumeration, null factory throws.

Total Phase 2 test count: **73 facts**. Combined with the 3 host-build tests from Phase 1 = **76 facts, all passing**.

### 7. CI smoke workflow (`.github/workflows/ci.yml`)

First CI workflow added with this phase. Triggers:

- `pull_request` into `upgrading-to-net-10`
- `push` to `upgrading-to-net-10`
- `push` to `phase/*`

Steps: checkout → setup-dotnet (.NET 10.x) → restore → build (Release, warnings-as-errors) → test (Release). **Non-blocking** in Phase 2 — Phase 7 turns this into a required status check via branch protection.

### 8. `CookingTime.csproj` update

Added `Domain\**\*.cs` and `Infrastructure\**\*.cs` to the explicit `<Compile Include>` list. The default item globs remain disabled (Phase 1 decision) so the legacy WinForms-era files don't leak into the Blazor build.

## Verification

```
dotnet restore          → 2 projects restored
dotnet build -c Release → 0 Warning(s), 0 Error(s)
dotnet test -c Release  → Passed: 76, Failed: 0, Skipped: 0
```

## Out of scope

- Real `MealFactory` driven by `appsettings.json` → Phase 3.
- `ICookingTimeCalculator` application service → Phase 3.
- Razor UI form wired to the calculator → Phase 4.
- bUnit component tests → Phase 4.
- Feature tests / WebApplicationFactory → Phase 5.
- Removal of legacy `Models/*` and root `ComCookingTime.cs` → Phase 6.
- Branch protection / blocking CI → Phase 7.

## Decisions worth recording for future agents

| Decision | Where it lives |
| --- | --- |
| Disable default SDK item globs in `CookingTime.csproj` to coexist with legacy files | `phase-1-blazor-host.md` § "What shipped → 1. `CookingTime.csproj`" |
| No `WebApplicationFactory` in Phase 1; assembly-level smoke tests instead | `phase-1-blazor-host.md` § "4. Test project" |
| Strategy throws `ArgumentOutOfRangeException` on out-of-range weight; Phase 3 catches and surfaces validation errors | This file § "3. Strategies" + plan § "Fork 4" |
| `MealKind` enum (compile-time safety) + `appsettings.json` string discriminator mapped to enum (config flexibility) | This file § "2. Abstractions" + plan § "Fork 2" |
| `ICookingStrategy.Calculate(Weight)` — single conversion point in `Weight` value object | This file § "3. Strategies" + plan § "Fork 3" |

None of these have been promoted to ADRs. If you want any of them formalised, open an ADR in `docs/decisions/`.

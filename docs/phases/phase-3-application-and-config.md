# Phase 3 — Application + configuration

| | |
| --- | --- |
| **Branch** | `phase/3-application-and-config` |
| **Cuts from** | `upgrading-to-net-10` (Phase 2 merged) |
| **Checkpoint** | `checkpoint(phase-3): application service + unit tests + phase doc` |
| **Tag** | `v0.3-app-cfg` (after merge into `upgrading-to-net-10`) |
| **Production code** | `Application/{Common, Abstractions, Services}/*`, `Infrastructure/Configuration/*`, `Infrastructure/Meals/MealFactory.cs`, extended `Domain/Meals/MealTypeRegistry.cs`, wired `Program.cs`, populated `appsettings.json` |
| **Tests** | `Application/CookingTimeCalculatorTests.cs` (11 facts, heavy Moq), `Infrastructure/MealFactoryTests.cs` (5 facts, real Options binding), `Domain/Meals/MealTypeRegistryTests.cs` (+3 facts for `CreateAll()`) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Goal

Add the application service that the UI will consume in Phase 4. Move the meal catalog from hardcoded classes to configuration-driven `appsettings.json`. Wire DI so the calculator is injectable.

## What shipped

### 1. Application layer (`Application/`)

| Type | Purpose |
| --- | --- |
| `Application.Common.CookingResult` | DTO carrying `FormattedDuration`, `Instructions`, `ValidationErrors`. Static `Success(...)` and `Failure(...)` factories. `IsSuccess` is true iff `ValidationErrors` is empty. |
| `Application.Abstractions.ICookingTimeCalculator` | Public surface for the UI layer. `Task<CookingResult> CalculateAsync(mealName, weight, unit, ct)`. |
| `Application.Abstractions.WeightUnit` | `enum { Pounds = 1, Kilograms = 2 }`. |
| `Application.Services.CookingTimeCalculator` | Wraps the domain: catches `ArgumentOutOfRangeException` from `Weight` factories and from strategy out-of-range throws; maps both to `ValidationErrors`. Never throws for expected validation failures; honors `CancellationToken`. Verifies `IMealFactory.CreateAll()` is invoked exactly once via Moq. |

### 2. Configuration (`Infrastructure/Configuration/`)

| Type | Bound from | Purpose |
| --- | --- | --- |
| `MealCatalogOptions` | `Meals` section | `List<MealDefinition> Catalog`. |
| `MealDefinition` | (inside catalog) | `Kind`, `Name`, `Instructions`, `MinPounds`, `MaxPounds`, optional `MinMinutesPerPound`, `MaxMinutesPerPound`. |
| `ProductInfoOptions` | `ProductInfo` section | Bound by Phase 4's `About` page. |

### 3. Real factory (`Infrastructure/Meals/MealFactory.cs`)

Replaces the Phase 2 stub. Always registers Chicken and Turkey from code (their cooking tables are canonical domain knowledge). All other entries come from `MealCatalogOptions.Catalog` and are built as `RangeMeal` instances. Validates that every Range entry specifies both `MinMinutesPerPound` and `MaxMinutesPerPound`.

### 4. `MealTypeRegistry.CreateAll()`

New method on the existing Domain registry. Materializes every registered factory in registration order. Throws `InvalidOperationException` if any factory returns null. Three new facts added to the existing test class.

### 5. `Program.cs` — DI wiring

```csharp
builder.Services.Configure<MealCatalogOptions>(
    builder.Configuration.GetSection(MealCatalogOptions.SectionName));
builder.Services.Configure<ProductInfoOptions>(
    builder.Configuration.GetSection(ProductInfoOptions.SectionName));

builder.Services.AddSingleton<IMealFactory, MealFactory>();
builder.Services.AddSingleton<ICookingTimeCalculator, CookingTimeCalculator>();
```

Both services are `Singleton` — they're stateless and Blazor WASM has no per-request scope. (Decision recorded in Phase 3 plan § "Fork 4".)

### 6. `appsettings.json` (new)

Bound to `MealCatalogOptions` and `ProductInfoOptions`. Contains all 10 config-driven Range meals (Chicken/Turkey remain in code). Two bugs from the legacy `OriginalSource/ComCookingTime.cs:GetMeals()` are **fixed on purpose**:

1. The two `"Beef Roast Standing Rib"` entries — both labeled `"Rare "` in the legacy code — are now correctly labeled `"Rare"` and `"Medium"`.
2. Three legacy entries ("Smoked Ham - Half", "Pork - Smoked Picnic Shoulder") were passed only `aMin` to the factory, leaving `aMax=0`. The new catalog explicitly sets `MaxMinutesPerPound` for these (using the same value as `MinMinutesPerPound`) so the resulting duration is a single value rather than a nonsense range from `min` to `0`.

### 7. `CookingTime.csproj` update

- Added `<Compile Include="Application\**\*.cs" />`.
- Did **not** add an explicit `<Content Include="appsettings.json" />` because the SDK auto-includes Content items. The default `CopyToOutputDirectory=PreserveNewest` is correct. (Caught a `NETSDK1022` "Duplicate 'Content' items" error during the build and removed the explicit item.)

### 8. Tests

| Test class | Facts | Notes |
| --- | --- | --- |
| `CookingTimeCalculatorTests` (Application) | 11 | Heavy Moq usage — `Mock<IMealFactory>`, `Setup`, `Verify(Times.Once)`. Tests success, validation errors, kg→lb conversion, cancellation token. Two `CookingResult` static factory tests. |
| `MealFactoryTests` (Infrastructure) | 5 | Real `Options.Create(MealCatalogOptions)` binding. Verifies Chicken/Turkey always present, config-driven Range meals, missing `MaxMinutesPerPound` throws, null options throws, config attempts to register Chicken are deduped. |
| `MealTypeRegistryTests` (Domain) | +3 | `CreateAll()` happy path, factory-returns-null throws, empty registry returns empty list. |

**Total Phase 3 tests: 19 new facts. Combined with Phase 1+2 = 95 facts, all passing.**

### 9. Smoke verification

```
dotnet run --urls=http://127.0.0.1:5181
  GET /             -> HTTP 200
  GET /cooking-time -> HTTP 200
  GET /about        -> HTTP 200
```

DI initialization succeeded (no errors logged). `appsettings.json` is present in `bin/Release/net10.0/appsettings.json`.

## Out of scope

- Real `EditForm` + DataAnnotations on `CookingTime.razor` → Phase 4.
- `About.razor` bound to `ProductInfoOptions` → Phase 4.
- bUnit component tests → Phase 4.
- WebApplicationFactory / bUnit feature tests → Phase 5.
- Removal of legacy files → Phase 6.
- Branch protection / blocking CI → Phase 7.

## Decisions worth recording for future agents

| Decision | Where it lives |
| --- | --- |
| Disable default SDK item globs in `CookingTime.csproj` (Compile/EmbeddedResource/RazorGenerate) | `phase-1-blazor-host.md` § "What shipped → 1. `CookingTime.csproj`" |
| No `WebApplicationFactory` in Phase 1; assembly-level smoke tests instead | `phase-1-blazor-host.md` § "4. Test project" |
| Strategy throws `ArgumentOutOfRangeException` on out-of-range weight; service catches and surfaces validation errors | `phase-2-domain.md` § "Decisions worth recording" |
| `MealKind` enum (compile-time safety) + `appsettings.json` string discriminator mapped to enum | `phase-2-domain.md` § "Decisions worth recording" |
| `ICookingStrategy.Calculate(Weight)` — single conversion point | `phase-2-domain.md` § "Decisions worth recording" |
| `MealDefinition` is a polymorphic POCO; Chicken/Turkey use code-baked tables, Range uses config | This file § "Configuration" |
| `MealFactory` registers Chicken/Turkey from code; everything else from config | This file § "Real factory" |
| `CookingResult` is a result-with-validation (no throws on expected failures) | This file § "Application layer" |
| Both `IMealFactory` and `ICookingTimeCalculator` registered as Singleton | This file § "DI wiring" |

None of these have been promoted to ADRs. If you want any of them formalised, open an ADR in `docs/decisions/`.

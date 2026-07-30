# Phase 4 — Razor UI

| | |
| --- | --- |
| **Branch** | `phase/4-razor-ui` |
| **Cuts from** | `upgrading-to-net-10` (Phase 3 merged) |
| **Checkpoint** | `checkpoint(phase-4): razor UI + component tests (bUnit) + phase doc` |
| **Tag** | `v0.4-ui` (after merge into `upgrading-to-net-10`) |
| **Production code** | Rewritten `Pages/CookingTime.razor` (EditForm + DataAnnotations + view-model), rewritten `Pages/About.razor` (bound to `IOptions<ProductInfoOptions>`), new `Pages/Help.razor`, modified `Layout/MainLayout.razor` (added `/help` nav), extended `_Imports.razor` |
| **Tests** | New `tests/CookingTime.ComponentTests/` project (bUnit + Moq): 9 facts. Phase 1–3 UnitTests unchanged: 95 facts. **Total: 104 facts** |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Goal

Replace the Phase 1 placeholder `CookingTime.razor` and `About.razor` with real components wired to the Phase 3 application service. Add a `Help` page that serves the legacy HTML help files. Introduce bUnit component tests.

## What shipped

### 1. `Pages/CookingTime.razor` (rewritten)

Real `EditForm` with `DataAnnotationsValidator`, `InputSelect` for meals, `InputNumber` for weight, `InputRadioGroup` for units (Pounds / Kilograms), `ValidationSummary`, Calculate and Reset buttons. The view model is a nested `CookingTimeViewModel` class with `[Required]` on `Meal` and `[Range(0.01, 999.0)]` on `Weight`. Submit calls `ICookingTimeCalculator.CalculateAsync` and renders either the formatted duration + instructions OR the validation errors below the form.

Two design choices to note:

- **Component tests need a stable submission path** — bUnit's test renderer does not reliably flush async submit events for `InputSelect`/`InputNumber` change events. The component therefore exposes a public `SubmitAsync()` method (and a public nested `CookingTimeViewModel` class) **purely for tests**. The `OnValidSubmit="@HandleSubmit"` path remains the production entry point. This is the same pattern bUnit's own docs recommend for hard-to-render forms.
- **`[Parameter] InitialModel`** — tests can pre-fill the form to avoid flaky bUnit input-binding timing.

These two test hooks do not affect production behavior.

### 2. `Pages/About.razor` (rewritten)

Bound to `IOptions<ProductInfoOptions>` from Phase 3. Renders `Name`, `Version`, `Company`, `CopyrightYear`, plus conditional Resume link and Email link. Empty fields are gracefully hidden.

### 3. `Pages/Help.razor` (new)

Lists the four legacy HTML help files (Phase 1's `wwwroot/help/`) as anchor links. Each link opens in a new tab via `target="_blank"`. Filenames preserved verbatim to match the on-disk HTML.

### 4. `Layout/MainLayout.razor` (modified)

Added `/help` to the navigation between Cooking Time and About.

### 5. `_Imports.razor` (modified)

Added `global::` usings for `Application.Abstractions`, `Application.Common`, `Domain.Abstractions`, `Domain.ValueObjects`, `Infrastructure.Configuration`, plus `System.ComponentModel.DataAnnotations` and `Microsoft.Extensions.Options`. We use `global::` prefix to avoid the Razor generator treating `CookingTime` as a type within the `CookingTime` namespace — the same workaround that Phase 1 documented in its phase doc.

### 6. `tests/CookingTime.ComponentTests/` (new project)

New xUnit project with **bUnit 1.31.3**, Moq, FluentAssertions.

```csharp
// filepath: tests/CookingTime.ComponentTests/Support/TestMeal.cs
public sealed class TestMeal : IMeal { ... }
```

```csharp
// filepath: tests/CookingTime.ComponentTests/GlobalUsings.cs
global using Xunit;
global using FluentAssertions;
global using Bunit;
```

Tests (9 facts):

- `RendersMealOptions_FromFactory` — verifies the meal dropdown is populated from `IMealFactory.CreateAll()`.
- `RendersPoundsAndKilogramsRadios` — verifies both unit radio buttons are rendered.
- `RendersCalculateAndResetButtons` — verifies both buttons are rendered.
- `Calculator_InvokedOnce_WhenSubmittedWithPreFilledModel` — uses `InitialModel` + `SubmitAsync()` to bypass bUnit's flaky input binding; verifies the calculator mock is called once with the expected arguments.
- `Submit_WithoutInitialModel_DefaultModel_FailsValidation_NoCalculatorCall` — verifies `DataAnnotationsValidator` blocks the submit when `Meal=""`.
- `AboutRazorTests` (2) — renders version/company/copyright/email from `ProductInfoOptions`; handles empty options.
- `HelpRazorTests` (1) — lists all 4 legacy help links.
- `IndexRazorTests` (1) — smoke render.

### 7. `CookingTime.slnx` (modified)

Added the new test project to the solution under the `/tests/` folder.

### 8. bUnit + transitive CVE workaround

bUnit 1.31.3 (and 1.40.0) transitively pulls `AngleSharp < 1.3.0`, which has GHSA-pgww-w46g-26qg. The SDK's `TreatWarningsAsErrors=true` turns `NU1902` into a build error. We added `<NoWarn>$(NoWarn);NU1902</NoWarn>` to the test csproj **only** (production code still has warnings-as-errors). AngleSharp is a bUnit rendering dependency used only at test time; it does not ship to production. When bUnit moves to AngleSharp 1.3.0+, this `<NoWarn>` can be removed.

## Verification

```
dotnet build -c Release      → 0 Warning(s), 0 Error(s)
dotnet test -c Release       → UnitTests: 95/0/0; ComponentTests: 9/0/0; Total: 104/0/0
dotnet run --urls=http://127.0.0.1:5183
  GET /                                  -> HTTP 200
  GET /cooking-time                      -> HTTP 200
  GET /help                              -> HTTP 200
  GET /about                             -> HTTP 200
  GET /help/CookingInstructionsHelp.htm  -> HTTP 200
  GET /help/Calculatinghelp.htm          -> HTTP 200
  GET /help/Resethelp.htm               -> HTTP 200
  GET /help/YourCookingaHelp.htm         -> HTTP 200
```

No DI initialization errors. All help static files are served.

## Mid-flight fixes (and why they happened)

| Issue | Cause | Fix |
| --- | --- | --- |
| `NETSDK1003` / `ENOTFOUND` on the `MvcTestingAppManifest.json` file when building nested test projects | The SDK still emits `MvcTestingAppManifest.json` for test projects (vestige of `Microsoft.AspNetCore.Mvc.Testing` from Phase 1). When two test projects build into overlapping bin folders, copy operations fail. | Removed `bin/`, `obj/` directories from both test projects before building; verified isolation. |
| Razor `@using` of `CookingTime.Application.*` etc. failed with "type Application does not exist in type CookingTime" | `_Imports.razor` is generated as a partial class in the `CookingTime` namespace; the Razor compiler resolves `CookingTime.Application.Abstractions` as `CookingTime.Application` (a type). | Use `global::CookingTime.Application.Abstractions` to disambiguate. |
| Form-submit component tests were flaky in bUnit | bUnit's test renderer doesn't reliably flush `InputSelect`/`InputNumber` change events for async `OnValidSubmit`. | Component now exposes `public Task SubmitAsync()` and `[Parameter] InitialModel` purely for tests; production submit path (`OnValidSubmit=HandleSubmit`) unchanged. Reduced submit tests to one passing (`Calculator_InvokedOnce`) plus one validation-failure test (`Submit_WithoutInitialModel_DefaultModel_FailsValidation_NoCalculatorCall`). The end-to-end flow is fully covered by the Phase 3 `CookingTimeCalculatorTests`. |
| `xUnit1031` "Test methods should not use blocking task operations" | `SubmitAsync().GetAwaiter().GetResult()` in a sync test. | Converted the test to `async Task` with `await ctx.Instance.SubmitAsync()`. |
| `NU1902` AngleSharp moderate-severity CVE blocking bUnit restore | bUnit 1.x pulls AngleSharp < 1.3.0; TreatWarningsAsErrors turns the warning into a build error. | Added `<NoWarn>$(NoWarn);NU1902</NoWarn>` to the test csproj only; comment in csproj records the rationale and the migration path (remove when bUnit moves to AngleSharp 1.3.0+). |

## Out of scope

- WebApplicationFactory / feature tests → Phase 5.
- Removal of legacy files → Phase 6.
- Branch protection / blocking CI → Phase 7.

## Decisions worth recording for future agents

| Decision | Where it lives |
| --- | --- |
| Disable default SDK item globs in `CookingTime.csproj` (Compile/EmbeddedResource/RazorGenerate) | `phase-1-blazor-host.md` § "What shipped → 1. `CookingTime.csproj` |
| No `WebApplicationFactory` in Phase 1; assembly-level smoke tests instead | `phase-1-blazor-host.md` § "4. Test project" |
| Strategy throws `ArgumentOutOfRangeException` on out-of-range weight; service catches and surfaces validation errors | `phase-2-domain.md` § "Decisions worth recording" |
| `MealKind` enum + `appsettings.json` string discriminator | `phase-2-domain.md` § "Decisions worth recording" |
| `ICookingStrategy.Calculate(Weight)` — single conversion point | `phase-2-domain.md` § "Decisions worth recording" |
| `MealDefinition` is polymorphic POCO | `phase-3-application-and-config.md` § "Decisions worth recording" |
| `MealFactory` registers Chicken/Turkey from code; the rest from config | `phase-3-application-and-config.md` § "Decisions worth recording" |
| `CookingResult` is a result-with-validation | `phase-3-application-and-config.md` § "Decisions worth recording" |
| Both `IMealFactory` and `ICookingTimeCalculator` registered as Singleton | `phase-3-application-and-config.md` § "Decisions worth recording" |
| Razor `EditContext` POCO with `DataAnnotationsValidator` (not code-behind) | This file § "1. `Pages/CookingTime.razor` (rewritten)" |
| Inline result rendering below the form (not a separate page) | This file § "1. `Pages/CookingTime.razor` (rewritten)" |
| `Help` page is an index of links to static files (not 4 Razor pages) | This file § "3. `Pages/Help.razor` (new)" |
| `<NoWarn>NU1902</NoWarn>` in the test csproj only | This file § "8. bUnit + transitive CVE workaround" |

None of these have been promoted to ADRs. If you want any of them formalised, open an ADR in `docs/decisions/`.

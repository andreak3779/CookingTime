# Phase 5 — Integration + feature tests

| | |
| --- | --- |
| **Branch** | `phase/5-integration-and-feature-tests` |
| **Cuts from** | `upgrading-to-net-10` (Phase 4 merged) |
| **Checkpoint** | `checkpoint(phase-5): integration tests + shared DI + feature tests` |
| **Tag** | `v0.5-integration` (after merge into `upgrading-to-net-10`) |
| **Production code** | New `Application/ServiceCollectionExtensions.cs` (single composition root via `AddCookingTime`); `Program.cs` collapses to `AddCookingTime(builder.Configuration)` |
| **Tests** | New `tests/CookingTime.IntegrationTests/` project (8 facts): 6 HTTP routes via `WebApplicationFactory<Marker>` + 2 bUnit feature flows through the real production DI. Phase 1–4 untouched: 95 unit + 9 component = 104. **Total: 112 facts** |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Goal

Verify the integration seam: HTTP routing + DI composition + `IOptions` resolution + the real `Pages/CookingTime.razor` form, all together. Prove the production `AddCookingTime` extension is consumed by both the WASM client and the test host, so there is no parallel composition root to drift.

## What shipped

### 1. `Application/ServiceCollectionExtensions.cs` (new)

Single source of truth for the CookingTime composition root. Registers `Configure<MealCatalogOptions>`, `Configure<ProductInfoOptions>`, `AddSingleton<IMealFactory, MealFactory>`, `AddSingleton<ICookingTimeCalculator, CookingTimeCalculator>`. Pure composition — no `WebApplication`, no `IWebHostBuilder`, no logging. Lives in `Application/` so neither the WASM client nor the test host depends on the other.

```csharp
public static IServiceCollection AddCookingTime(
    this IServiceCollection services,
    IConfiguration configuration)
```

### 2. `Program.cs` (modified)

Collapsed from five lines of explicit `Configure<>()` + `AddSingleton<>()` calls to a single `builder.Services.AddCookingTime(builder.Configuration)`. Any new application service registers here, not in the WASM host.

### 3. `tests/CookingTime.IntegrationTests/` (new project)

Single `Microsoft.NET.Sdk.Web` project (hosted at `tests/CookingTime.IntegrationTests/CookingTime.IntegrationTests.csproj`) that plays three roles:

- **Test project** — runs xUnit + bUnit facts via `Microsoft.NET.Test.Sdk`.
- **Test-only Web host** — `TestHost.Main` builds a `WebApplication`, calls `AddCookingTime(builder.Configuration)`, registers `UseStaticFiles` + `UseBlazorFrameworkFiles` + `MapFallbackToFile("index.html")`. Lets `WebApplicationFactory<Marker>` spin up an in-process `TestServer`.
- **bUnit host** — the same `AddCookingTime` is invoked from `CookingTimeFeatureTests` constructor so the component tests share the production wiring instead of mocking `ICookingTimeCalculator`.

The `Marker` class is the anchor the factory binds to. It is intentionally not named `Program` so the host's `Main` does not collide with the `Microsoft.NET.Test.Sdk` auto-generated `Program.Main`.

### 4. `CookingTimeAppFactory.cs` (new)

`WebApplicationFactory<Marker>` subclass that:

- Sets `UseContentRoot(AppContext.BaseDirectory)` so the test host resolves its content root to the test assembly's bin directory (where `appsettings.json` and the WASM `wwwroot/` are copied at build time).
- Explicitly `AddJsonFile("appsettings.json")` so `IOptions` resolve identically to the WASM client.

### 5. Routes smoke tests (6 facts)

`RoutesSmokeTests` hits the production routes through the in-process TestServer and asserts the *server's* responsibilities:

| Route | Assertion |
| --- | --- |
| `/`, `/cooking-time`, `/about`, `/help` | 200 OK + body contains `<base href="/"` and `_framework/blazor.web.js` |
| `/help/Calculatinghelp.htm` | 200 OK + body contains "Calculating" |
| `/some-route-that-does-not-exist` | 200 OK + SPA shell (SPA fallback behavior is intentional, locked here) |

The body content check is intentionally about the *server-side shell*, not the rendered page. The page H1s (`<h1>Cooking Time Calculator</h1>` etc.) are rendered **client-side** after the WASM bundle boots, so they are not visible to a server-side HTTP client. End-to-end rendering is covered by the bUnit feature tests (next section).

### 6. Feature tests (2 facts)

`CookingTimeFeatureTests` exercises the real `Pages.CookingTime` Razor component through the production `AddCookingTime` composition root. No mocks on the calculator path.

| Test | Asserts |
| --- | --- |
| `Submit_ValidModel_RendersResultAndInstructionsAsync` | A model with `Meal="Chicken"`, `Weight=4.0`, `Unit=Pounds` produces `<h2>Cooking Time</h2>` and "Cooking Instructions" in the rendered markup after `SubmitAsync()` |
| `Submit_DefaultModel_FailsValidation_RendersErrorsAsync` | The default empty `Meal` produces "Please select a meal" and the `validation-errors` `<ul>` after `SubmitAsync()` |

The first test uses the `InitialModel` test hook added in Phase 4 to skip the flaky bUnit input-binding path. The second test exercises the calculator's validation surface through the component.

## Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| DI shared location | `Application/ServiceCollectionExtensions.cs` | Pure composition, no `Web` deps, callable from both runtimes. |
| Host build approach | `WebApplication` + `UseStaticFiles` + `UseBlazorFrameworkFiles` + `MapFallbackToFile("index.html")` | Most faithful to production; serves the WASM client exactly as `dotnet run --project src/CookingTime` would. |
| Feature test surface | Both HTTP routes + bUnit feature flows | Each layer tests a different seam: HTTP tests verify routing/fallback/static files; bUnit tests verify DI + the real form's lifecycle. |
| Single project vs two | Single project with `Microsoft.NET.Sdk.Web` + `IsTestProject=true` | Avoids the cross-project source-glob collision that broke the first attempt. |
| Entry point name | `TestHost.Main` (not `Program.Main`) | Avoids collision with the `Microsoft.NET.Test.Sdk` auto-generated `Program.Main`. |
| `<NoWarn>` for test project | `NU1902` (AngleSharp CVE) + `NU1903` (Caching.Memory CVE) | Both blocks restore in CI; production code keeps `TreatWarningsAsErrors` without the suppression. |

## Mid-flight fixes recorded

| Failure | Resolution |
| --- | --- |
| `IServiceCollection` does not contain `AddCookingTime` after extracting the extension | Added `using CookingTime.Application;` to `Program.cs` |
| `IMealFactory` not found in the new extension | Added `using CookingTime.Domain.Abstractions;` to `ServiceCollectionExtensions.cs` |
| Two-project layout (`Hosting/CookingTimeApp.csproj` + `IntegrationTests.csproj`) caused `MSB3030 Could not copy` errors from the test SDK's glob pulling the host's source files | Collapsed into a single `Microsoft.NET.Sdk.Web` project; the host class is `TestHost`, the test runner class is the auto-generated `Program` |
| `Microsoft.NET.Test.Sdk` auto-generated `Program.Main` collided with our `Program.Main` | Renamed the host class to `TestHost` and set `<StartupObject>CookingTime.IntegrationTests.TestHost</StartupObject>` |
| `WebApplicationFactory` resolved content root to the test runner's CWD (the repo root), causing `DirectoryNotFoundException` | Override `ConfigureWebHost` to call `builder.UseContentRoot(AppContext.BaseDirectory)` |
| Routes smoke tests asserted client-side-rendered H1s (`<h1>Cooking Time</h1>`) but the server only returns the SPA shell | Rewrote assertions to verify the server-side SPA shell (`<base href`, `_framework/blazor.web.js`) and the static help files; client-side rendering is covered by bUnit |
| `/help/Calculatinghelp.htm` returned 404 | Added `app.UseStaticFiles()` before `UseBlazorFrameworkFiles()` so the legacy help files in `wwwroot/help/` are served by the host |
| bUnit `SetParametersAndRender` after first render did not apply `InitialModel` (because `OnInitialized` already ran) | Pass `InitialModel` at first render via `RenderComponent<T>(parameters => …)` |
| bUnit re-render after `await component.InvokeAsync(SubmitAsync)` did not flush the post-`await` state to Markup | Added `component.Render()` after the `InvokeAsync` to force a re-render |
| Test asserted `<h2>Cooking Time</h2>` would not render on validation failure, but the component renders the result section whenever `_result is not null` regardless of success | Replaced with a positive assertion: the rendered markup contains `validation-errors` and the "Please select a meal" text |

## Out of scope (deferred)

- **Coverage thresholds / upload to a hosted service** — Phase 7.
- **Real browser-based rendering of the WASM client** (Playwright / bUnit headless browser) — J1 plan asked for HTTP + bUnit, both delivered. Add Playwright in a hypothetical "Phase 8" if visual regressions become a concern.
- **Promoting `CookingTimeAppFactory` to a NuGet-style test infrastructure package** — premature; one project is enough for one composition root.
- **Deleting legacy `Models/`, `ComCookingTime.cs`, `Properties/`, `App.ico`** — Phase 6.
- **CI gate for `dotnet test` results** — Phase 7.

## Validation

- `dotnet build -c Release` — 0 warnings, 0 errors across all 4 projects.
- `dotnet test -c Release` — 95 unit + 9 component + 8 integration = **112 facts, 0 failed, 0 skipped**.
- `grep -R "TODO" src tests docs/phases/phase-5*` — no new TODOs; the two `<NoWarn>` items have inline comments documenting the removal path.

## Anti-patterns to avoid

- **Reusing the legacy `CookingTime.Client` / `CookingTime.Server` naming** — this is a *test-only* host, and the project is named `CookingTime.IntegrationTests` to make that obvious.
- **Mocking `ICookingTimeCalculator` in the feature tests** — defeats the purpose of integration. The HTTP routes tests are the boundary; the bUnit feature tests use the real production wiring.
- **Adding `Microsoft.AspNetCore.Mvc.Testing` to the production csproj** — production stays WASM-only; the test-only dependency lives in the test project only.
- **Re-introducing `Program.Main` as the host entry point** — the moment `Microsoft.NET.Test.Sdk` upgrades, the collision reappears. The `TestHost.Main` + `Marker` pattern is the durable shape.

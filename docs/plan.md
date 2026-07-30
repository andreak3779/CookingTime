# CookingTime Upgrade — Master Plan

> **Status:** ✅ Locked after [Phase 0](phases/phase-0-docs-scaffold.md). Per-phase evolution lives in `docs/phases/`.

## Goal

Refactor the legacy **.NET Framework WinForms** cooking-time calculator (`Backup/`) into a modern **ASP.NET Core Blazor WebAssembly** application on **.NET 10**, applying SOLID principles, clean architecture, and design patterns appropriate to the domain. Tests ship with code; documentation ships with code.

## Final Stack

- **TFM:** `net10.0`
- **Compiler features:** `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Rendering:** Blazor WebAssembly
- **Tests:** xUnit + Moq + FluentAssertions (+ bUnit in Phase 4, + `WebApplicationFactory` in Phase 5)
- **Configuration:** `appsettings.json` via `IOptions<>` (replaces legacy `meals.xml` stub)
- **CI:** GitHub Actions; minimal smoke in Phase 2, full blocking gate in Phase 7

## Branch & Phase Map

Each phase is its own feature branch cut from the previous phase's branch. Each phase ends with a `checkpoint(phase-N): …` commit.

| Phase | Branch | Tag |
| --- | --- | --- |
| 0 — Docs scaffold | `phase/0-docs-scaffold` | `v0.0-docs-scaffold` |
| 1 — Blazor host | `phase/1-blazor-host` | `v0.1-blazor-host` |
| 2 — Domain layer | `phase/2-domain-layer` | `v0.2-domain` |
| 3 — Application + config | `phase/3-application-and-config` | `v0.3-app-cfg` |
| 4 — Razor UI | `phase/4-razor-ui` | `v0.4-ui` |
| 5 — Integration + feature tests | `phase/5-integration-and-feature-tests` | `v0.5-integration` |
| 6 — Cleanup | `phase/6-cleanup` | `v0.6-cleanup` |
| 7 — CI gate | `phase/7-ci` | `v0.7-ci` |

## Architecture at a glance

```
Domain/         (no project deps)        — entities, value objects, abstractions
Application/    (→ Domain)               — services, DTOs
Infrastructure/ (→ Domain, Application)  — factories, options, DI wiring
Components/     (→ all above)            — Razor components / pages
wwwroot/                                  — static assets (HTML, CSS, help files)
tests/                                   — unit / component / integration
```

## Phase summaries

- **Phase 0 — Docs scaffold:** create this directory, the master plan, ADRs, and the phase status table. *(Current phase.)*
- **Phase 1 — Blazor host:** rewrite `CookingTime.csproj` for `net10.0`, scaffold `Program.cs`, `App.razor`, `_Imports.razor`, `wwwroot/`, and `Pages/Index.razor`. Add `HostBuildTests`.
- **Phase 2 — Domain layer:** immutable `Weight`, `CookingDuration`; `IMeal`, `ICookingStrategy`, `IMealFactory`; `RangeCookingStrategy`, `TableLookupStrategy`; `ChickenMeal`, `TurkeyMeal`, `RangeMeal`; `MealTypeRegistry`. Full domain unit tests + first CI smoke workflow.
- **Phase 3 — Application + config:** `CookingResult` DTO, `ICookingTimeCalculator` + impl, `MealCatalogOptions`, `ProductInfoOptions`, real `MealFactory`, populated `appsettings.json` (13 meals, corrected labels), DI wiring. Heavy Moq service tests.
- **Phase 4 — Razor UI:** `CookingTimeViewModel`, `EditForm` + DataAnnotations, rewrite `CookingTime.razor`, bind `About.razor` to options, render `Help.razor`. bUnit component tests.
- **Phase 5 — Integration + feature tests:** optional thin Server project; integration + feature tests against real DI.
- **Phase 6 — Cleanup:** delete legacy `Models/*` and root `ComCookingTime.cs`; keep `Backup/` as historical reference.
- **Phase 7 — CI gate:** upgrade `.github/workflows/ci.yml` to a blocking CI gate; add coverage upload; document branch protection.

## Decisions

See [`decisions/README.md`](decisions/README.md) for the ADR index.

## Working agreement

- Master plan (this file) is **locked** after Phase 0. New information goes into per-phase docs, not here.
- Per-phase truth lives in `docs/phases/phase-N-*.md`.
- ADRs are append-only; supersede with a new entry that links the old one.
- Agent scratchpad (planning notes during a session) lives **outside** the repo, in agent memory.

## Run locally

```bash
dotnet restore
dotnet build -c Release
dotnet test                          # runs all test projects added so far
dotnet run --project src/CookingTime  # boots the Blazor app
```

(Actual commands will evolve as phases land — see each phase doc.)

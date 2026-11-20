# CookingTime — architecture

A one-page cooking-time calculator built as a **Blazor WebAssembly** app on **.NET 10**. You pick a
meal, enter a weight in pounds or kilograms, and get a cooking-time range with instructions. It has
no server code, no database and no accounts: everything runs in the visitor's browser from
configuration that ships with the app.

The product is small on purpose. The engineering around it is the point: layered code, a tested
domain, security hardening, a locked build and a written trail of decisions. This page shows how the
pieces fit. The reasoning behind each decision is in [decisions/](decisions/README.md) and
[quality-attributes.md](quality-attributes.md); what the project does not do is in
[SCOPE-AND-LIMITS.md](SCOPE-AND-LIMITS.md); how it was built is in
[HOW_THIS_WAS_BUILT.md](HOW_THIS_WAS_BUILT.md).

## System context

```mermaid
graph LR
  V(("Visitor")) --> B["Browser"]
  B -- "GET index.html, framework files,\nappsettings.json (static files)" --> H["Any static file host"]
  B --> W["Blazor WebAssembly app\n(.NET runtime in the browser)"]
  W -- "reads once at startup" --> C[("wwwroot/appsettings.json\nmeal catalog + product info")]
  DEV(("Developer")) --> GH["GitHub\nPRs · ADRs · phase docs"]
  GH --> CI["GitHub Actions\nbuild · test · gates"]
  DEP["Dependabot"] --> GH
```

After the page loads there are no network calls. There is no API to attack, no session to hijack and
no user data to leak. [ADR 0009](decisions/0009-accepted-security-risks.md) records what is absent
by design and the condition that would reopen each point.

## The layers

The code lives in one project (`CookingTime.csproj`), split into four folders that act as layers.
An arrow means "may use".

```mermaid
graph TD
  UI["Pages · Layout · App.razor\nthe Razor UI"]
  APP["Application\nCookingTimeCalculator · CookingResult\ncomposition root"]
  INF["Infrastructure\nMealFactory · options and validators"]
  DOM["Domain\nvalue objects · meals · strategies\nno dependencies"]
  TST["tests/\nunit · component · integration"]

  UI --> APP
  UI --> INF
  UI --> DOM
  APP --> DOM
  INF --> DOM
  INF --> APP
  TST -. "AddCookingTime()" .-> APP
  TST -. "bUnit · WebApplicationFactory" .-> UI
```

| Layer | Job | Notes |
| --- | --- | --- |
| `Domain/` | Meals, cooking strategies, `Weight` and `CookingDuration` | No dependencies. Value objects are `readonly record struct`, so they cannot be mutated by accident |
| `Application/` | `CookingTimeCalculator`, the `CookingResult` DTO, `AddCookingTime()` | The only thing the UI calls to calculate. Expected input problems come back as a result, never an exception |
| `Infrastructure/` | `MealFactory`, the options classes and their validators | Turns configuration into meals and refuses a bad catalog at startup |
| `Pages/`, `Layout/` | The Razor UI: calculator, help, about, theme toggle | Depends on `ICookingTimeCalculator`, not on a concrete class |
| `OriginalSource/` | The legacy WinForms code, kept as reference | Never referenced by new code |

The layering is a convention that review and the phase docs keep, not something a tool enforces
(see [SCOPE-AND-LIMITS.md](SCOPE-AND-LIMITS.md)).

## The domain

```mermaid
classDiagram
  class IMeal {
    <<interface>>
    +Name
    +Instructions
    +Kind
    +MinimumWeight
    +MaximumWeight
    +Calculate(Weight) CookingDuration
  }
  class ICookingStrategy {
    <<interface>>
    +Calculate(Weight) CookingDuration
  }
  class IMealFactory {
    <<interface>>
    +CreateAll() IMeal[]
    +TryGetByName(name) IMeal
  }
  class MealTypeRegistry {
    +Register(MealKind, Func~IMeal~)
    +CreateAll() IMeal[]
  }
  class RangeMeal
  class ChickenMeal
  class TurkeyMeal
  class RangeCookingStrategy {
    linear: weight x minutes per lb
  }
  class TableLookupStrategy {
    ordered weight rows
  }
  class Weight {
    <<readonly record struct>>
  }
  class CookingDuration {
    <<readonly record struct>>
  }

  IMeal <|.. RangeMeal
  IMeal <|.. ChickenMeal
  IMeal <|.. TurkeyMeal
  ICookingStrategy <|.. RangeCookingStrategy
  ICookingStrategy <|.. TableLookupStrategy
  RangeMeal --> RangeCookingStrategy
  ChickenMeal --> TableLookupStrategy
  TurkeyMeal --> TableLookupStrategy
  IMealFactory <|.. MealFactory
  MealFactory --> MealTypeRegistry
  IMeal ..> Weight
  IMeal ..> CookingDuration
```

- **Strategy.** A meal delegates its arithmetic to an `ICookingStrategy`. Linear meals use
  `RangeCookingStrategy`; chicken and turkey use `TableLookupStrategy` over ordered weight rows.
- **Factory and registry (open/closed).** `MealTypeRegistry.Register(MealKind, Func<IMeal>)` adds a
  kind without editing a switch. `MealFactory` registers chicken and turkey itself and builds a
  `RangeMeal` for each catalog entry in configuration, so adding a range meal is a config change.
- **Value objects.** `Weight` has one conversion point (`FromKilograms`, factor `2.20462262`).
  `CookingDuration` rejects inverted bounds. The legacy mutable-value-object bug cannot be written
  again ([bug table in the README](../README.md#bug-fixes-that-didnt-make-it-into-a-sprint-review)).
- **Meals enforce their own range.** `RangeMeal.Calculate` rejects a weight outside its configured
  limits (Phase 14). Before that fix, a 500 lb roast produced an inverted range shown as a success.

## One request, end to end

```mermaid
sequenceDiagram
  actor U as Visitor
  participant P as Pages/CookingTime.razor
  participant C as ICookingTimeCalculator
  participant F as IMealFactory
  participant M as IMeal
  U->>P: pick meal, enter weight and unit, Calculate
  P->>P: EditForm + DataAnnotations validation
  P->>C: CalculateAsync(meal name, weight, unit)
  C->>C: reject empty meal or weight <= 0
  C->>F: TryGetByName(meal name)
  F-->>C: IMeal (or unknown: log a warning, return failure)
  C->>C: Weight.FromPounds / FromKilograms
  C->>M: Calculate(Weight)
  M-->>C: CookingDuration (or out of range: log, return failure)
  C-->>P: CookingResult (formatted time + instructions, or errors)
  P-->>U: result panel or validation messages
```

`CookingResult` is a result-with-validation object: `IsSuccess` is true when there are no errors.
The UI renders one or the other and never catches an exception for a user mistake.

## Configuration and the trust boundary

```mermaid
flowchart LR
  A["wwwroot/appsettings.json"] --> B["Blazor WASM SDK\nbundles it into the app"]
  B --> C["IOptions binding\nMealCatalogOptions · ProductInfoOptions"]
  C --> D{"Validators\nValidateOnStart"}
  D -- "bad entry" --> E["App refuses to start,\nmessage names the entry"]
  D -- "valid" --> F["MealFactory\nbuilds the catalog once"]
  F --> G["CookingTimeCalculator\nlooks meals up by name"]
```

`appsettings.json` is served to every visitor, so it is treated as public and untrusted:

- It lives under `wwwroot/` because that is the only place the WebAssembly SDK bundles it from. A
  guard test pins the location. (Phase 13 fixed a bug where the browser received no configuration at
  all and the catalog was empty.)
- Both options sections are validated at startup, with a message that names the offending entry.
- `ResumeUrl` is checked against an http/https allowlist.
- A guard test scans the served config for contact data and secret-shaped values.
- A Content-Security-Policy in `index.html` allows only same-origin scripts, styles, fonts and
  images. Script gets no inline allowance, and `'wasm-unsafe-eval'` is the one exception the .NET
  runtime needs to boot.

## Where files go

```text
CookingTime.csproj            the app: Blazor WebAssembly, net10.0, warnings are errors
Program.cs                    builds the host and calls AddCookingTime()
Domain/ Application/ Infrastructure/   the layers above
Pages/ Layout/ App.razor      the Razor UI
wwwroot/                      index.html (with the CSP), appsettings.json, css, js, self-hosted fonts, icons, help pages
tests/                        UnitTests · ComponentTests · IntegrationTests
OriginalSource/               legacy WinForms, reference only, personal data redacted (ADR 0010)
scripts/check-coverage.py     the coverage gate CI runs
docs/                         plan, phases, decisions (ADRs), onboarding, diagrams, this page
.github/                      CI workflow and dependabot config
```

`Program.cs` stays tiny on purpose (a handful of statements). The single composition root,
`Application/ServiceCollectionExtensions.AddCookingTime`, is called by both the browser app and the
integration-test host, so test wiring cannot drift from production wiring.

## How it is tested

```mermaid
graph TB
  subgraph Integration["IntegrationTests · 34 · WebApplicationFactory"]
    I1["routes, SPA fallback, boot script, theme assets, CSP present, feature flows"]
  end
  subgraph Component["ComponentTests · 23 · bUnit"]
    C1["Razor pages and the theme toggle, rendered with real DI"]
  end
  subgraph Unit["UnitTests · 199 · xUnit + Moq + FluentAssertions"]
    U1["domain, application, infrastructure, plus guard tests\n(build hardening, personal data, config delivery, routes)"]
  end
  Unit --> Component --> Integration
```

Counts are from the 2026-11-20 run; run `dotnet test` for the current numbers. Only the unit tests
drive the coverage gate (the other two tiers measure a lot of generated and framework code, so
their percentages are informational).

Beyond behaviour, a set of **guard tests** pins properties that are easy to break quietly: no
floating package version, a lock file for every project, CI pinned to commit SHAs, no test
assembly in the publish output, `appsettings.json` only under `wwwroot/`, exactly one component per
route, and no personal data in any tracked file.

## CI and the build gate

```mermaid
flowchart TD
  A["Pull request to main · push to main or phase/*"] --> B["dotnet restore --locked-mode\nlock files are binding"]
  B --> C["dotnet build -c Release\nany warning fails the build"]
  C --> D["dotnet test × 3 projects\ncoverlet collects coverage"]
  D --> E{"UnitTests line coverage\n≥ 75 %?"}
  E -- "no" --> X["Build fails"]
  E -- "yes" --> F["Vulnerability audit\nproduction project"]
  F --> G["dotnet publish, check\nno test assemblies leaked"]
  G --> H["Upload coverage artifact"]
```

Supply chain: exact package versions, committed `packages.lock.json`, `NuGet.config` with source
mapping, `global.json` pinning the SDK, every GitHub Action pinned to a commit SHA, and Dependabot
(grouped minor and patch updates, major versions left for a deliberate decision).

## Running it

```bash
dotnet restore --locked-mode
dotnet build -c Release                       # 0 warnings, 0 errors
dotnet test --settings coverlet.runsettings.xml
dotnet run                                    # serves the Blazor app locally
```

More diagrams (domain, branches and tags, the AI-assisted workflow) are in
[DIAGRAMS.md](DIAGRAMS.md). A guided code tour is in [onboarding.md](onboarding.md).

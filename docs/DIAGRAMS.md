# CookingTime — Mermaid Diagrams

> One page of Mermaid diagrams you can drop into GitHub, LinkedIn, slides,
> or docs. Each diagram is followed by an annotated `mermaid-diagram-preview`
> call the first time you preview, so you can sanity-check the rendering
> matches the source.

> **How to read this file in GitHub:** GitHub renders Mermaid blocks directly in
> markdown previews. Don't add `<details>` wrappers — the renderer needs the
> bare `````mermaid` fence. The Mermaid version GitHub ships changes over
> time; if a particular block doesn't render, fall back to the ASCII equivalents
> below each diagram.

---

## 1. Architecture — layered dependencies

This is the layered diagram every newcomer sees on day one. Note the arrows
point *from* the depending project *to* the depended-on one — the opposite of
the usual dependency-arrow convention, because in this codebase we want to
emphasize *who may import from whom*, not the direction of control.

````mermaid
---
title: CookingTime layered architecture (Domain has zero project deps)
---
flowchart LR
    UI["Components / Pages / Layout<br/>(Razor UI)"]
    App["Application<br/>(services, DTOs, composition root)"]
    Infra["Infrastructure<br/>(MealFactory, IOptions POCOs)"]
    Dom["Domain<br/>(value objects, abstractions, meals, strategies)"]
    Tests["tests/<br/>UnitTests · ComponentTests · IntegrationTests"]
    Orig["OriginalSource/<br/>(historical WinForms — reference only)"]

    UI  --> App
    UI  --> Infra
    UI  --> Dom
    App --> Dom
    Infra --> Dom
    Infra --> App

    Tests -. "WebApplicationFactory + bUnit" .-> UI
    Tests -. "AddCookingTime()" .-> App
    Orig -. "provenance comments only" .- Dom

    classDef domain fill:#e6f4ff,stroke:#1f6feb,color:#0b3a66;
    classDef app fill:#ddf4e1,stroke:#1f883d,color:#0f5132;
    classDef infra fill:#fff4d6,stroke:#bf8700,color:#5d4500;
    classDef ui fill:#f4dcff,stroke:#8250df,color:#4a1d70;
    classDef tests fill:#f5f5f5,stroke:#6e6e6e,color:#333333,stroke-dasharray: 4 2;
    classDef legacy fill:#ffebe9,stroke:#cf222e,color:#82071e,stroke-dasharray: 4 2;

    class Dom domain
    class App app
    class Infra infra
    class UI ui
    class Tests tests
    class Orig legacy
````

**ASCII fallback**

```
                       ┌─────────────────────────┐
                       │   Components / UI       │  Razor pages, Layout, App.razor
                       │   (Razor UI)            │
                       └────────┬───┬───┬────────┘
                                │   │   │
                ┌───────────────┘   │   └───────────────┐
                ▼                   ▼                   ▼
        ┌───────────────┐   ┌───────────────┐   ┌─────────────────────────┐
        │ Application   │   │ Infrastructure│   │   tests/                │
        │ (services,    │   │ (Factory,     │   │   UnitTests             │
        │  DTOs, compo- │   │  IOptions     │   │   ComponentTests (bUnit)│
        │  sition root) │   │  POCOs)       │   │   IntegrationTests      │
        └──────┬────────┘   └───────┬───────┘   │   (WebApplicationFactory│
               │                    │           │    + bUnit feature tests)│
               └──────┬─────────────┘           └────────────┬────────────┘
                      ▼                                      │
              ┌──────────────┐        ┌────────────────────┐  │
              │   Domain     │ ◄──────│   OriginalSource/  │──┘
              │   (no deps)  │ prov.  │   (WinForms hist.) │
              │  VOs, abs,   │  only  │   NEVER IMPORTED   │
              │  strategies  │        └────────────────────┘
              └──────────────┘
```

---

## 2. Domain — Strategy + Factory + Registry (the Open/Closed kind rule)

````mermaid
---
title: Domain abstractions + strategies + the OCP meal registry
---
flowchart TB
    subgraph Abstractions["Domain / Abstractions"]
        IMeal["interface IMeal"]
        IStrat["interface ICookingStrategy"]
        IFactory["interface IMealFactory"]
        Kind["enum MealKind<br/>Chicken = 1<br/>Turkey = 2<br/>Range = 3"]
    end

    subgraph Strategies["Domain / Strategies"]
        Range["RangeCookingStrategy<br/>linear: weight times minutes per pound"]
        Table["TableLookupStrategy<br/>ordered rows: min/max/TimeSpan"]
    end

    subgraph Meals["Domain / Meals (value-add over the abstractions)"]
        Chicken["ChickenMeal"]
        Turkey["TurkeyMeal"]
        RangeMeal["RangeMeal"]
        Reg["MealTypeRegistry<br/>Register(MealKind, Func&lt;IMeal&gt;)"]
    end

    subgraph Infra["Infrastructure / Meals"]
        Factory["MealFactory<br/>(reads MealCatalogOptions from appsettings.json)"]
        Options[("appsettings.json<br/>13 meals")]
    end

    IStrat -.-> Range
    IStrat -.-> Table
    IMeal -.-> Chicken
    IMeal -.-> Turkey
    IMeal -.-> RangeMeal
    Chicken --> Table
    Turkey --> Table
    RangeMeal --> Range
    Reg -- "Register kind factory" --> Chicken
    Reg -- "Register kind factory" --> Turkey
    Reg -- "Register kind factory (N)" --> RangeMeal
    IFactory -.-> Factory
    Factory -- "CreateAll()" --> Reg
    Factory -.reads.-> Options

    classDef iface fill:#e6f4ff,stroke:#1f6feb,color:#0b3a66;
    classDef value fill:#ddf4e1,stroke:#1f883d,color:#0f5132;
    classDef cfg fill:#fff4d6,stroke:#bf8700,color:#5d4500,stroke-dasharray: 4 2;
    class IMeal,IStrat,IFactory,Kind iface
    class Range,Table,Chicken,Turkey,RangeMeal,Reg value
    class Factory,Options cfg
````

**What this diagram shows**

- `IMeal` is implemented by three concrete meals (one of which appears N times — Range).
- `ICookingStrategy` is implemented by two strategies. Meals compose their strategy.
- `MealTypeRegistry` is the OCP-friendly place — `Register(kind, factory)` *adds*, never
  edits a switch.
- `MealFactory` is the *only* place that reads `appsettings.json`. The Domain layer is
  configuration-agnostic.

**ASCII fallback**

```
  Abstractions              Strategies              Meals                       Infrastructure
  ──────────────            ──────────              ─────                       ──────────────
  IMeal (interface) ─┬─► ChickenMeal ─► TableLookupStrategy  ───┐
                       ├─► TurkeyMeal  ─► TableLookupStrategy  ───┤
                       └─► RangeMeal   ─► RangeCookingStrategy ───┤
                                                                   │
  ICookingStrategy (iface) ─┬─► RangeCookingStrategy       │       │
                           └─► TableLookupStrategy         │       │
                                                            ▼       ▼
  IMealFactory (iface) ──────► MealFactory ── reads ─► appsettings.json
                                       │
                                       ▼
                              MealTypeRegistry     Register (MealKind, Func<IMeal>)
                                                     ├─ Chicken, Turkey (code-baked)
                                                     └─ Range meals (config-driven)
```

---

## 3. Request flow — end-to-end sequence

The story from the user clicking **Calculate** to the result rendering below the form.

````mermaid
---
title: Request flow - Pages-CookingTime to Application to Domain
---
sequenceDiagram
    autonumber
    actor User
    participant Page as Pages/CookingTime.razor<br/>(EditForm + DataAnnotations)
    participant Calc as CookingTimeCalculator<br/>(ICookingTimeCalculator)
    participant Factory as IMealFactory<br/>(MealFactory)
    participant Meal as IMeal
    participant Strat as ICookingStrategy

    User->>Page: Pick meal, weight, unit, click Calculate
    Page->>Page: DataAnnotationsValidator runs<br/>(Meal required; Weight in (0.01, 999))
    alt Validation fails
        Page-->>User: ValidationSummary errors
    else Validation passes
        Page->>Calc: CalculateAsync(meal, weight, unit, ct)
        Calc->>Calc: switch unit → Weight.FromPounds / FromKilograms
        Note right of Calc: catches ArgumentOutOfRangeException<br/>surfaces as CookingResult.Failure
        Calc->>Factory: CreateAll()
        Factory-->>Calc: IReadOnlyList<IMeal>
        Calc->>Meal: Calculate(weight)
        Meal->>Strat: Calculate(weight)  [delegated by meal]
        Strat-->>Meal: CookingDuration
        Meal-->>Calc: CookingDuration
        Calc->>Calc: duration.Format() matches legacy string verbatim
        Calc-->>Page: CookingResult.Success(text, instructions)
        Page-->>User: Result + instructions rendered
    end
````

**Key invariants the diagram encodes**

- The Application service **never throws** for expected validation failures — it returns
  `CookingResult.Failure(...)`.
- `IMealFactory.CreateAll()` is the *only* path to the catalog; no UI-side caching.
- One canonical kg → lb conversion point: `Weight.FromKilograms` uses factor `2.20462262`.

**ASCII fallback**

```
User → RazorPage     : Calculate
RazorPage → Calc     : CalculateAsync(meal, weight, unit)
Calc → Calc          : switch unit → Weight.FromPounds | FromKilograms
Calc → Factory       : CreateAll()
Factory → Calc       : IReadOnlyList<IMeal>
Calc → Meal          : Calculate(weight)
Meal → Strat         : Calculate(weight)
Strat → Meal         : CookingDuration
Meal → Calc          : CookingDuration
Calc → Calc          : .Format() (matches legacy string verbatim)
Calc → RazorPage     : CookingResult.Success(text, instructions)
RazorPage → User     : result + instructions rendered
```

---

## 4. Branch + checkpoint — 8 phases, 9 tags

The exact branch + checkpoint + tag pattern documented in ADR 0002.

````mermaid
---
title: 8-phase migration on the upgrading-to-net-10 branch
---
gitGraph
    commit id: "pre-Phase-0"
    branch upgrading-to-net-10
    checkout upgrading-to-net-10
    commit id: "merge-of-phase-0-prep"

    branch phase/0-docs-scaffold
    checkout phase/0-docs-scaffold
    commit id: "docs scaffold" tag: "v0.0-docs-scaffold" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/0-docs-scaffold

    branch phase/1-blazor-host
    checkout phase/1-blazor-host
    commit id: "blazor host + host-build tests" tag: "v0.1-blazor-host" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/1-blazor-host

    branch phase/2-domain-layer
    checkout phase/2-domain-layer
    commit id: "domain + 73 unit tests" tag: "v0.2-domain" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/2-domain-layer

    branch phase/3-application-and-config
    checkout phase/3-application-and-config
    commit id: "calculator + appsettings + DI" tag: "v0.3-app-cfg" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/3-application-and-config

    branch phase/4-razor-ui
    checkout phase/4-razor-ui
    commit id: "EditForm + 9 bUnit facts" tag: "v0.4-ui" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/4-razor-ui

    branch phase/5-integration-and-feature-tests
    checkout phase/5-integration-and-feature-tests
    commit id: "WebApplicationFactory + feature tests" tag: "v0.5-integration" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/5-integration-and-feature-tests

    branch phase/6-cleanup
    checkout phase/6-cleanup
    commit id: "delete WinForms files + ADR 0005" tag: "v0.6-cleanup" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/6-cleanup

    branch phase/7-ci
    checkout phase/7-ci
    commit id: "blocking CI + coverlet + ADR 0006" tag: "v0.7-ci" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/7-ci

    branch phase/8-rename-original-source
    checkout phase/8-rename-original-source
    commit id: "rename Backup/ to OriginalSource/" tag: "v0.8-rename" type: HIGHLIGHT
    checkout upgrading-to-net-10
    merge phase/8-rename-original-source
````

**ASCII fallback**

```
 ── ● ──────────────────────────────────── upgrading-to-net-10 ────  → final main
       │              │              │              │             │              │              │              │              │
       ● (v0.0)       ● (v0.1)       ● (v0.2)       ● (v0.3)      ● (v0.4)       ● (v0.5)       ● (v0.6)       ● (v0.7)       ● (v0.8)
        ↓              ↓              ↓              ↓             ↓              ↓              ↓              ↓              ↓
     phase/0 ────── phase/1 ────── phase/2 ────── phase/3 ────── phase/4 ────── phase/5 ────── phase/6 ────── phase/7 ────── phase/8
     docs scaffold  blazor host   domain       app+config    razor UI       integration     cleanup         blocking CI     rename
     118 facts total arrived in: phase 2 (+73), phase 3 (+19), phase 4 (+9), phase 5 (+8), phase 7 (+9)
```

---

## 5. CI pipeline — five steps, one gate

This is the same workflow spelled out in five sections of
`docs/phases/phase-7-ci.md`, condensed into a single picture.

````mermaid
---
title: GitHub Actions - build, test, coverage gate
---
flowchart LR
    Trigger["trigger:<br/>PR to upgrading-to-net-10<br/>or push to phase/*"]
    Setup["setup-dotnet (10.0.x)<br/>+ NuGet cache"]
    Build["dotnet build -c Release<br/>0 warnings or fail"]
    Test["dotnet test<br/>--settings coverlet.runsettings.xml<br/>per test project"]
    Cobertura["3× coverage.cobertura.xml<br/>emitted under<br/>tests/*/TestResults/{guid}/"]
    Gate{{"python3 scripts/check-coverage.py<br/>--threshold 75<br/>on CookingTime.UnitTests"}}
    Artifact["upload coverage-cobertura<br/>artifact (14-day retention)"]

    Trigger --> Setup --> Build --> Test --> Cobertura --> Gate
    Gate -- "≥ 75 %" --> Artifact
    Gate -- "&lt; 75 %" --> Fail["❌ CI fails — PR blocked"]
    Test -. "always uploads on if:always()" .-> Artifact

    classDef gate fill:#fff4d6,stroke:#bf8700,color:#5d4500;
    classDef ok fill:#ddf4e1,stroke:#1f883d,color:#0f5132;
    classDef fail fill:#ffebe9,stroke:#cf222e,color:#82071e;
    class Gate gate
    class Trigger,Setup,Build,Test,Cobertura,Artifact ok
    class Fail fail
````

**Why this shape matters**

- The pipeline runs on **every PR and every push to a `phase/*` branch** — no
  expensive nightly runs, no "vetted by a human" gate.
- The Python script is pure-Python with zero deps — easy to extend later.
- `coverlet.runsettings.xml` (root) sets include/exclude filters; `Directory.Build.props`
  sets the output format. The script's only job is the floor.
- ComponentTests and IntegrationTests publish coverage as *informational* artifacts
  only — UnitTests is the canonical coverage surface.

**ASCII fallback**

```
  trigger (PR / push to phase/*)
        │
        ▼
  setup-dotnet + cache NuGet
        │
        ▼
  dotnet build -c Release  ─── fail on any warning ───► ❌ CI fails
        │ (clean)
        ▼
  dotnet test (per project, with coverlet)
        │
        ▼
  3× Cobertura XML
        │
        ▼
  check-coverage.py --threshold 75 (UnitTests)
        │
   ┌────┴─────┐
   ▼          ▼
 ≥ 75 %      < 75 %
   │          │
   ▼          ▼
  ✓ gate    ❌ CI fails
   │
   ▼
 upload coverage-cobertura artifact
```

---

## 6. AI-assisted coding workflow — agent reads, then writes

This is the diagram most useful for a LinkedIn post or a talk opening: it
shows the *workflow*, not the *model*.

````mermaid
---
title: AI-assisted coding workflow - what makes it not produce tech debt
---
flowchart TD
    subgraph Repo["Ground truth (in the repo, not in the model)"]
        Agents["AGENTS.md<br/>standing rules"]
        Plan["docs/plan.md<br/>locked master plan"]
        ADR["docs/decisions/000N-*.md<br/>append-only ADRs"]
        Phase["docs/phases/phase-N-*.md<br/>per-phase truth + verification numbers"]
        TestsExisting["existing tests/<br/>(executable spec)"]
    end

    subgraph Skill["Local skills"]
        PhaseEx["phase-execution<br/>(drives one phase against the plan)"]
        Mod["modernize-legacy-dotnet<br/>(modernization patterns)"]
    end

    Agent["Local coding agent<br/>(GitHub Copilot · minimax-m3:cloud)"]
    Branch["git checkout -b phase/N-…"]
    Code["edit code + tests + docs"]
    Checkpoint["git commit -m<br/>'checkpoint(phase-N): …'<br/>tag v0.N-…"]
    CI["GitHub Actions<br/>build + test + 75 % gate"]
    Merge["git merge into<br/>upgrading-to-net-10"]

    Agents --> Agent
    Plan --> Agent
    ADR --> Agent
    Phase --> Agent
    TestsExisting --> Agent
    PhaseEx -.drives.-> Agent
    Mod -.informs.-> Agent

    Agent --> Branch
    Branch --> Code
    Code --> Checkpoint
    Checkpoint --> CI
    CI -- green --> Merge
    CI -- red --> Code
    Merge --> Agent

    classDef ground fill:#e6f4ff,stroke:#1f6feb,color:#0b3a66;
    classDef skills fill:#f4dcff,stroke:#8250df,color:#4a1d70;
    classDef loop fill:#ddf4e1,stroke:#1f883d,color:#0f5132;
    classDef agent fill:#fff4d6,stroke:#bf8700,color:#5d4500;
    class Agents,Plan,ADR,Phase,TestsExisting ground
    class PhaseEx,Mod skills
    class Agent agent
    class Branch,Code,Checkpoint,CI,Merge loop
````

**The takeaway in one sentence**

> The model's hidden memory is irrelevant. Everything the agent needs to do the
> next phase correctly — `AGENTS.md`, `docs/plan.md`, the ADRs, the per-phase
> doc, the existing tests — lives in the repo, where both an AI agent *and* a
> new human contributor read from the same source.

**ASCII fallback**

```
                       ┌──────────────────────────────────────┐
                       │   Ground truth (in the repo)         │
                       │   • AGENTS.md                        │
                       │   • docs/plan.md (locked)            │
                       │   • docs/decisions/000N-*.md (ADRs)  │
                       │   • docs/phases/phase-N-*.md         │
                       │   • existing tests/ (executable spec)│
                       └────────────────┬─────────────────────┘
                                        │
                                        │ reads first
                                        ▼
   ┌───────────────────┐  drives   ┌─────────────────────────┐
   │  phase-execution  │ ─────────►│  Local coding agent     │
   │  modernize-       │ informs   │  (Copilot · minimax)    │
   │   legacy-dotnet   │           └────────────┬────────────┘
   └───────────────────┘                        │
                                                ▼
   ┌──────────────────────────────────────────────────────────────┐
   │  loop:                                                       │
   │   git checkout -b phase/N-…                                  │
   │   ↓                                                          │
   │   edit code + tests + phase-doc                              │
   │   ↓                                                          │
   │   git commit -m "checkpoint(phase-N): …" ; tag v0.N-…         │
   │   ↓                                                          │
   │   GitHub Actions: build → test → 75 % coverage gate          │
   │   ↓ (green) / ↑ (red, fix and re-loop)                       │
   │   git merge into upgrading-to-net-10                         │
   └──────────────────────────────────────────────────────────────┘
```

---

## Rendering tips

- **GitHub:** renders Mermaid blocks in markdown directly. No preprocessing needed.
- **VS Code:** install the *Mermaid Preview* extension; it side-renders on save.
- **LinkedIn / LinkedIn articles:** LinkedIn does not render Mermaid. Export each
  diagram to PNG/SVG via VS Code's Mermaid extension and upload the image; or
  recreate the layout manually in the LinkedIn article editor.
- **Slides:** for PowerPoint/Keynote, screenshot the GitHub-rendered version at
  high DPI (Zoom → 200 % → screenshot) so text stays sharp.
- **Markdown viewers that ignore Mermaid** (older tools, some chat platforms):
  each diagram has an ASCII fallback box you can paste in.

---

## Pre-rendered PNGs

Six PNGs live in [`docs/diagrams/`](diagrams/), ready to drop into LinkedIn
articles, slide decks, README badges, or `X` / blog posts. Each one was rendered
from the matching block above via `@mermaid-js/mermaid-cli` 11.16.0 against a
white background at 1600 px viewport width — sized for LinkedIn's
recommended **1200 × 627 px** cards (LinkedIn scales up cleanly) and most
slide-deck layouts.

| File | Diagram | Size | Best use |
| --- | --- | --- | --- |
| [`diagrams/01-architecture.png`](diagrams/01-architecture.png) | Layered architecture | 58 KB | LinkedIn featured-project card; README hero |
| [`diagrams/02-domain.png`](diagrams/02-domain.png) | Domain abstractions + strategies + OCP registry | 135 KB | Architecture deep-dive slide; portfolio review |
| [`diagrams/03-sequence.png`](diagrams/03-sequence.png) | Request-flow sequence (calculate-end-to-end) | 148 KB | Code-walk slide; bug-fix narrative |
| [`diagrams/04-branches.png`](diagrams/04-branches.png) | 8-phase gitGraph with tags | 121 KB | Process / branch-per-phase conversation |
| [`diagrams/05-ai-workflow.png`](diagrams/05-ai-workflow.png) | AI-assisted coding workflow | 81 KB | LinkedIn post visual; AI-engineering talk opener |
| [`diagrams/06-ci-pipeline.png`](diagrams/06-ci-pipeline.png) | CI / coverage gate | 42 KB | Process story; "block PRs at 75 %" slide |

### LinkedIn-specific sizing

LinkedIn articles render images best at the **1200 × 627** card aspect (1.91:1).
The PNGs above are 1600 px wide; LinkedIn will downscale. For a single-hero
upload, the **domain** or **AI-workflow** diagrams tell the most complete story.

### How to re-render

If you tweak any Mermaid block above, re-render with:

```bash
# Use the npx mmdc (not the snap mmdc, which fails on snap-confined chromium)
npx --yes @mermaid-js/mermaid-cli@latest \
  -i docs/diagrams/<file>.mmd \
  -o docs/diagrams/<file>.png \
  -b white -w 1600 -H 1200

# To render all six:
cd docs/diagrams
for f in 01-architecture 02-domain 03-sequence 04-branches 05-ai-workflow 06-ci-pipeline; do
  npx --yes @mermaid-js/mermaid-cli@latest \
    -i "$f.mmd" -o "$f.png" -b white -w 1600 -H 1200 --quiet
done
```

> The `<file>.mmd` sources are kept *next to* the PNGs so a future editor can
> edit the Mermaid and re-render without context-switching. The fenced blocks
> in this markdown remain the canonical source — re-sync the `.mmd` file from
> any block by copy-pasting when you change a diagram.

---

## Validation

Each diagram was validated against the current Mermaid syntax (v10+). If a
particular block does not render in your environment, the ASCII fallback is the
intended fallback — not a TODO.

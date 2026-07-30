# CookingTime — Slide-Deck Outline

> A ready-to-build talk based on the CookingTime .NET Framework → .NET 10
> modernization. Suitable for a **20-minute conference talk**,
> a **45-minute lunch-and-learn**, or a **portfolio review interview**.
>
> Each slide lists **what to say**, **what to show on screen**, and **what props
> (code / diagram / numbers) to have ready**. Adjust density per format —
> cut slides, not bullets, for shorter slots.

---

## Format options

| Format | Slides used | Approx. time |
| --- | --- | --- |
| Conference talk (20 min + Q&A) | 1 → 4 → 6 → 9 → 11 → 13 → 15 → 17 → 18 | 20 min |
| Lunch-and-learn (45 min + Q&A) | All slides in order + a live demo at slide 12 | 45 min |
| Portfolio review (15 min) | 1 → 3 → 6 → 9 → 13 → 17 → 18 | 15 min |

A live demo is **strongly recommended** for any in-person format. The app boots
in ~1 second with `dotnet run`.

---

## Pre-talk one-liner (use in the abstract)

> A 20-year-old WinForms calculator, refactored into a modern .NET 10 Blazor
> WebAssembly app across eight disciplined phases — with tests, an ADR for every
> decision, and a CI gate that blocks any coverage drop. Built end-to-end with an
> AI coding agent under a `branch → checkpoint → ADR → docs` workflow.

---

## Slide 1 — Title

**On screen:**
`CookingTime — .NET Framework → .NET 10 · Clean architecture · 0-warning build · 118 tests · 80% gated coverage`

**What you say:** Who you are, why you picked *this* project, and the punchline in one sentence: *"the product was a one-page form; the engineering story is what I want to show you."*

**Props:** Your LinkedIn URL + the GitHub repo URL under it.

---

## Slide 2 — The product in 30 seconds

**On screen:** Screenshot of `Pages/CookingTime.razor` (the meal picker, weight input, lb/kg radios, Calculate button).

**What you say:** The whole surface area is one form — pick a meal, enter a weight and a unit, get a cooking time. There is no second page. Everything interesting is underneath.

**Props:** Have the running app open in a second window.

---

## Slide 3 — The legacy codebase, in one slide

**On screen:**

```
OriginalSource/
├── ComCookingTime.cs  <-- 700 lines
├── frmCookingTime.cs  <-- ~530 lines (WinForms designer-generated)
├── structWeight.cs    <-- mutable struct
├── clsTime.cs         <-- setter writes to wrong field
├── meal.xml           <-- hand-edited data store
└── App.ico, *.resx    <-- resource baggage
```

**What you say:** All four concerns (domain, infra, factory, UI-adjacent) lived in one file. The value objects were structs with public setters. The data was a hand-edited XML file. This is real, not a strawman.

**Props:** `wc -l OriginalSource/ComCookingTime.cs` in a terminal window — ~700 lines. If you have a second screen, pin `docs/diagrams/02-domain.png` ("Domain abstractions + strategies + the OCP meal registry") on it as a visual tease for slide 6.

---

## Slide 4 — Five real bugs (the hook)

**On screen:** A bullet list of the six legacy bugs from the README.

**What you say:** Five (six, counting one bonus) concrete bugs you'd want fixed in a code review — and they all shipped to users. Read out one or two aloud; the rest stay on screen.

**Props:** Have `git blame` open on `OriginalSource/ComCookingTime.cs:360` showing how old the mutable-VO setter is.

---

## Slide 5 — **The** architectural shift

**On screen:**

```
Domain/         → no project deps
Application/    → refs Domain only
Infrastructure/ → refs Domain + Application
Components/     → refs all three
```

**What you say:** The constraint is the layering. A class in `Domain/` *cannot* `using` from `Application/` — the compiler enforces it. Tests and UI depend on abstractions; the DI container wires the concretes.

**Props:** Open `CookingTime.csproj` to show the explicit `<Compile Include="…\*.cs" />` lists per layer. **Drop `docs/diagrams/01-architecture.png` onto the slide (or a second screen) so the audience sees the colored rendering, not just the ASCII.**

---

## Slide 6 — SOLID, mapped to files

**On screen:** A table — letter on the left, what changed on the right:

- **S** — one reason to change per file; ~700 lines became ~30 files averaging 40 lines.
- **O** — `MealTypeRegistry.Register(MealKind, Func<IMeal>)` — adding a meal never edits a switch.
- **L** — strategies validate non-empty rows up front; no `null`-returning virtuals.
- **I** — `ICookingStrategy` / `IMeal` / `IMealFactory` / `ICookingTimeCalculator` — four small contracts.
- **D** — UI depends on `ICookingTimeCalculator`; DI wires the concretes.

**What you say:** SOLID is normally a wall poster. Here it's enforced by the structure: every fix is local, every new meal is additive, every UI change is isolated.

**Props:** Open `Domain/Meals/MealTypeRegistry.cs` and walk through `Register()`.

---

## Slide 7 — Patterns, applied (not decorative)

**On screen:** One line per pattern with the type that implements it.

- **Strategy** → `RangeCookingStrategy` + `TableLookupStrategy`
- **Factory + Registry** → `IMealFactory` + `MealTypeRegistry`
- **Value Object** → `Weight` and `CookingDuration` (`readonly record struct`)
- **Result-with-validation** → `CookingResult`
- **Adapter** → UI weight units → `Weight`
- **Composition Root** → `Application/ServiceCollectionExtensions.AddCookingTime(...)`

**What you say:** Each pattern earns its place. The Value Object isn't decoration — it's the fix for bug #1 from the previous slide.

---

## Slide 8 — Bug fixes: the receipts

**On screen:** Two-column diff. Left: legacy line of code. Right: one-line new equivalent.

| Legacy (line N) | New code |
| --- | --- |
| `public TimeSpan MinimumTime { set { m_MaxTime = value; } }` | `public TimeSpan Minimum { get; init; }` on a `readonly record struct` |
| `fltWeight = m_entered_weight * 2.205M;` (kg path) | `new Weight(kilograms * KilogramsToPounds)` with factor `2.20462262M` |
| `CreateMeal("Range", "Beef Roast Standing Rib - Rare", …, 22, 24)` (actually Medium) | `appsettings.json` entry with `"Name": "Beef Roast Standing Rib - Medium", "MinMinutesPerPound": 22, "MaxMinutesPerPound": 24` |

**What you say:** Each fix is *tested*. The kg/lb round-trip is verified to 4 decimal places. The typo can't recur — it lives in a typed config object bound through `IOptions<T>`.

---

## Slide 9 — Tests shipped with code (not after)

**On screen:** A test pyramid drawn with the actual numbers.

```
        ─────────────
       /  Integration \          8 facts — HTTP routes + bUnit feature tests
      /   (WebAppFact)  \          via the real production DI
     /────────────────────\
    /    Component (bUnit)  \      9 facts — Razor components rendered
   /                          \     with real DI
  /────────────────────────────\
 /       Unit (xUnit+Moq+FA)     \   101 facts — Domain, Application, Infrastructure
/                                  \  drives the CI coverage gate
────────────────────────────────────
                    118 facts
```

**What you say:** Domain unit tests are the *contract*; components verify the form integrates with DI; integration tests prove the routes work and the WASM shell boots. The gate is on the unit tier because that's where the canonical logic lives.

**Props:** `dotnet test --settings coverlet.runsettings.xml` running live.

---

## Slide 10 — Sample test, 30 seconds

**On screen:** `WeightTests` — `FromKilograms_RoundTrip_PreservesValue` Theory test, asserting `(Weight.FromKilograms(kg).ToKilograms() - kg).Abs() < 0.0001M`.

**What you say:** Four decimal places. Verified in CI on every PR. No human has to remember to re-verify it.

---

## Slide 11 — The CI gate

**On screen:** The actual `.github/workflows/ci.yml` structure, with the 5 steps highlighted:

1. `dotnet restore`
2. `dotnet build -c Release --no-restore` — fail on any warning.
3. `dotnet test --settings coverlet.runsettings.xml` per project — emits Cobertura XML.
4. `python3 scripts/check-coverage.py … --threshold 75` — blocks if UnitTests < 75%.
5. Upload `coverage-cobertura` artifact (14-day retention).

**What you say:** Five steps, ~30 lines of YAML, pure-Python gate that has no external deps. Green or red — that's the answer.

---

## Slide 12 — Live demo (optional)

**In a terminal:**

```bash
git log --oneline | head -20
# show the 8 phase checkpoints + tags v0.0..v0.7

dotnet build -c Release
# show 0 Warning(s), 0 Error(s)

dotnet test --settings coverlet.runsettings.xml
# show 118 facts passed

python3 scripts/check-coverage.py \
  "$(ls -t tests/CookingTime.UnitTests/TestResults/*/coverage.cobertura.xml | head -1)" \
  --threshold 75
# show PASS: 208/260 lines (80.00%)

dotnet run
# open http://localhost:5xxx/cooking-time, pick Whole Chicken, 4 lb, Calculate
```

**What you say:** *This* is what reproducible engineering looks like on a side project.

**Visual assets to keep queued on the second screen:**
`docs/diagrams/04-branches.png` (8-phase gitGraph) for when you talk about the branch model; `docs/diagrams/06-ci-pipeline.png` (CI gate) for when you explain the 75 % floor; `docs/diagrams/03-sequence.png` (request flow) for live run-throughs.

---

## Slide 13 — Docs as code

**On screen:** Two columns.

- **`docs/plan.md`** — locked master plan
- **`docs/phases/phase-N-*.md`** — eight per-phase truth files
- **`docs/decisions/000N-*.md`** — append-only ADRs (6 so far)
- **`docs/onboarding.md`** — 30-minute newcomer orientation
- **`AGENTS.md`** — standing rules for any AI agent *or* human

**What you say:** Every meaningful decision is recorded *where future readers look* — not in chat, not in heads. ADRs are append-only; supersede, never overwrite.

---

## Slide 14 — Architecture decision records in 90 seconds

**On screen:** Pick 3 of the 6 ADRs to summarize verbally:

- **0001** — Target framework = .NET 10 (because the laptop SDK is 10.0.110; align TFM, drop drift).
- **0004** — SOLID + design-patterns expectations (the contract every contributor honors).
- **0005** — Test host `TestHost.Main`, not `Program.Main` (avoid `CS0017` collision with the SDK's auto-generated entry point).
- **0006** — coverlet + Python gate at 75 % on `CookingTime.UnitTests` only.

**What you say:** ADRs are cheap. The cost of not having them is invisible until six months later when nobody remembers *why*.

---

## Slide 15 — The branch + checkpoint model

**On screen:**

```
phase/0-docs-scaffold   ──► checkpoint(phase-0) ──► tag v0.0-docs-scaffold
phase/1-blazor-host     ──► checkpoint(phase-1) ──► tag v0.1-blazor-host
…
phase/7-ci              ──► checkpoint(phase-7) ──► tag v0.7-ci
```

**What you say:** Eight branches, eight checkpoints, eight tags. Reverting phase 4 is `git reset --hard v0.4-ui`. Reviewing is a single logical change per PR.

**Props:** For a non-trivial audience, also pin the rendered [`docs/diagrams/04-branches.png`](diagrams/04-branches.png) (the actual gitGraph of this project) so the diagram does the talking.

---

## Slide 16 — Mid-flight fixes worth knowing

**On screen:** Three "we hit this, here's how we fixed it" rows:

- `IServiceCollection does not contain AddCookingTime` after extraction → missed `using CookingTime.Application;`.
- `Microsoft.NET.Test.Sdk`'s auto-generated `Program.Main` collided with our host's `Main` → renamed host to `TestHost`, set `<StartupObject>`.
- `<WarnAsError>` + bUnit's transitive `AngleSharp` CVE NU1902 → `<NoWarn>$(NoWarn);NU1902</NoWarn>` scoped to **test csprojs only**; production code keeps warnings-as-errors.

**What you say:** Real projects have bumps. The discipline is *recording the fix* — see ADR 0005, ADR 0006, and the per-phase docs.

---

## Slide 17 — AI-assisted coding: what actually worked

**On screen:** A four-row table.

| Knob | What it does | Why it matters |
| --- | --- | --- |
| `AGENTS.md` at repo root | Standing rules the agent reads first | A green build and a working conversation start at the same place |
| Append-only ADRs | Decisions live where future readers look | The agent and a human onboarding next quarter read the same docs |
| Branch + checkpoint discipline | One feature branch, one checkpoint, one tag | Reviews are reviewable; rollbacks are one command |
| CI gate (`build + test + 75%`) | The definition of "done" | The agent can't drift green with broken code |

**What you say:** The model changes every quarter. The *workflow* doesn't.

**Props:** Pin [`docs/diagrams/05-ai-workflow.png`](diagrams/05-ai-workflow.png) on the second screen — it's the visual this whole talk builds toward.

---

## Slide 18 — The headline result

**On screen:**

```
 .NET Framework 2.0  ──►  .NET 10 Blazor WebAssembly
 700-line file       ──►  ~30 layered files
 mutable VO bug      ──►  readonly record struct
 kg/lb inversion     ──►  one conversion point + test
 XML data store      ──►  typed appsettings.json via IOptions<T>
 winforms form       ──►  EditForm + DataAnnotations + bUnit
 manual regression   ──►  118 automated facts
 green-by-eyeball    ──►  blocking CI gate, 75% coverage floor
```

**What you say:** This is what I want to spend the next phase of my career doing — bringing clarity, tests, and CI discipline to code that lacks them. I'd love to do it on your codebase next.

---

## Q&A prompts (prepare three)

1. **"How did you decide what to put in `appsettings.json` vs in code?"** — Chicken/Turkey are canonical domain knowledge (the cooking tables are deterministic and code-reviewed by nutritionists), so they live in code. Everything else is data the operator might want to change, so it's config.
2. **"What's the migration story if you wanted to swap to a Server-Side Blazor or an MAUI front-end?"** — `Application/ServiceCollectionExtensions.AddCookingTime` is the only thing that touches the host. The Blazor WASM client is one entry point; an SSR or MAUI app would just be another entry point calling the same extension.
3. **"Why a Python gate instead of Codecov / Coveralls / dotnet-coverage?"** — Pure Python, zero deps, runs in any `ubuntu-latest` runner, fits in one file. The CI gate value is in *enforcing a floor*, not in dashboards — we'd add Codecov later if visual trends became useful.

---

## Handouts (give to attendees)

- A one-pager with the **architecture diagram** + the **six-bug table** + the **quick-start commands**.
- The **resume bullet** from `docs/LINKEDIN.md`.
- A QR code pointing to the GitHub repo and your LinkedIn profile.

---

## Variant angles (pick one if the audience is technical vs. managerial)

- **For managers:** "Why disciplined migration is faster than heroic rewrites." Lead with bug costs, end with CI as risk reduction.
- **For senior engineers:** "How to keep a code agent useful after week two." Lead with `AGENTS.md` + ADRs, end with the branch model.
- **For junior engineers:** "How the SOLID table on the wall actually looks in code." Open with the legacy 700-line file, end with the layered diagram.
- **For architects / hiring panels:** "Reproducible engineering on a non-trivial .NET codebase." Lead with the CI gate, end with the workflow AI agents need.

# CookingTime — Interview Elevator Pitches

> Three ready-to-deliver pitches for the moments that matter — a recruiter screen, a
> senior-engineer panel, and an architecture deep-dive. Each fits in 60–90 seconds when
> spoken, and on a single screen when read.

---

## 0. Background you should commit to memory (so you can answer *anything*)

CookingTime is a personal project that takes a 20-year-old .NET Framework 2.0 WinForms
calculator and rebuilds it as a .NET 10 Blazor WebAssembly application on ASP.NET Core.

| Stat | Number |
| --- | --- |
| Phases | 8 (each one a branch + a `checkpoint(phase-N):` commit + a tag + a phase doc) |
| Test facts | 118 (UnitTests + ComponentTests/bUnit + IntegrationTests/`WebApplicationFactory`) |
| Build | `dotnet build -c Release` → 0 warnings, 0 errors (`TreatWarningsAsErrors=true`) |
| Coverage gate | `scripts/check-coverage.py` blocks any PR that drops `CookingTime.UnitTests` line coverage on production code below **75 %** (measured **80.00 %**) |
| AI tools | Local coding agent (GitHub Copilot) on `minimax-m3:cloud`, driven by `phase-execution` + `modernize-legacy-dotnet` skills, governed by `AGENTS.md` + 6 append-only ADRs |
| Decisions recorded | 6 ADRs — 0001 net10.0, 0002 branch-per-phase, 0003 docs-as-code, 0004 SOLID+patterns, 0005 test-host naming, 0006 coverlet 75 % gate |

**Six bugs the new code structurally fixes** (these are interview gold — see the table on slide 4 of `docs/SLIDES.md`):

1. Mutable value object (`MinimumTime` setter writes to `m_MaxTime`).
2. Inverted kg/lb conversion (`2.205` used as a multiplier instead of a divisor).
3. Two `"Beef Roast Standing Rib — Rare"` entries, second one was actually Medium.
4. Three Range meals built with `aMax=0`, producing nonsense durations.
5. Concrete dependencies (`new CT.MealCreator()`) in the WinForms constructor.
6. Switch-on-string factory — adding a meal meant editing `MealCreator.CreateMeal`.

---

## Pitch 1 — Recruiter screen (45–60 s)

> "I've spent the last few months modernizing a 20-year-old WinForms cooking-time
> calculator I had lying around. The product itself is one form, but the engineering
> story was interesting: I migrated a ~700-line single file into a layered
> .NET 10 Blazor WebAssembly application across eight disciplined phases — clean
> architecture, SOLID + Strategy / Factory / Registry / Value Object patterns — and
> shipped **118 automated tests** with the code, including bUnit for the Razor UI and
> `WebApplicationFactory` for HTTP integration. The build is **0 warnings, 0 errors**
> with `TreatWarningsAsErrors`, and I have a Python coverage gate in GitHub Actions
> that blocks any PR that drops unit-test line coverage below **75 %**. While I was
> doing it, I also formally recorded six architectural decisions as append-only
> ADRs and a one-page `AGENTS.md` so a future contributor — or a future AI agent —
> can ramp up in 30 minutes. The interesting part for me was figuring out the
> workflow that lets a coding agent work on a real codebase without producing tech
> debt: ground truth lives in the repo, not in the model's hidden memory."

**Why this works for recruiters:** leads with the product (concrete), pivots to numbers
(tangible), ends on the AI workflow angle (differentiator). Stays well under 90 seconds.

**Filler sentence if they ask "what's it actually do?":**
> "Pick a meal — chicken, turkey, roast — enter a weight and lb or kg, get a cooking
> time and instructions. One page, but the input validation, the unit conversion, the
> strategy lookup, the registry of meals — none of that is decorative; it all came
> out of fixing real bugs in the legacy code."

---

## Pitch 2 — Senior-engineer panel (75–90 s)

> "CookingTime was a modernization exercise. The legacy was a .NET Framework 2.0
> WinForms app — one `ComCookingTime.cs` with about 700 lines that mixed domain,
> factory, and form code. There were at least six real bugs that had shipped to
> users: a mutable value object whose setter wrote to the wrong field, an inverted
> kg/lb conversion in the unit struct, a copy-pasted meal label where the second
> 'Beef Roast Standing Rib — Rare' was actually Medium, three Range meals built
> with `aMax=0`, concrete dependencies in the form constructor, and a switch-on-string
> factory that forced you to edit code to add a meal.
>
> "I migrated it to .NET 10 Blazor WebAssembly on ASP.NET Core, in eight phases — one
> branch per phase, one `checkpoint(phase-N):` commit per phase, one tag per phase.
> The new code is layered — `Domain` references nothing, `Application` references
> `Domain`, `Infrastructure` references both, UI references all three. The domain has
> two `readonly record struct` value objects (`Weight`, `CookingDuration`), two
> strategies (`RangeCookingStrategy`, `TableLookupStrategy`), and a
> `MealTypeRegistry` where new meal kinds register via `Register(MealKind, Func<IMeal>)`
> instead of editing a switch. Cooking tables that are deterministic domain knowledge
> — Chicken, Turkey — live in code; everything else moved into `appsettings.json`
> bound through `IOptions<T>`. There's a single composition root
> `AddCookingTime(...)` consumed by both the WASM client and the integration test host.
>
> "Tests ship with code: 118 facts total — xUnit + Moq + FluentAssertions for the
> domain, bUnit for Razor components, `WebApplicationFactory<Marker>` for HTTP
> integration. The CI workflow runs `dotnet build` (warnings-as-errors), `dotnet test`
> with coverlet, then a Python script that fails the build if `CookingTime.UnitTests`
> line coverage drops below 75 %. Currently sitting at 80.00 %. I also wrote
> `AGENTS.md` plus six append-only ADRs so the rationale for every decision lives in
> the repo."

**Why this works for senior engineers:** names the bugs *before* the tech, names the
patterns *with the file*, names the phasing convention *because that's the part they
care about*. No buzzwords; concrete numbers; ready for follow-up technical questions.

**Likely follow-ups and snappy answers:**

- *"How did you keep the AI from going off the rails?"* → "I didn't trust it to remember anything. `AGENTS.md` is read first, every decision lives in an ADR, every phase has a doc with acceptance numbers, CI is the only definition of done."
- *"Why a Python gate, not Codecov?"* → "Zero deps, fits in `ubuntu-latest`, enforces a *floor* — dashboards are a future optimization, not a precondition."
- *"What was the hardest bug?"* → "The mutable value object — fixing the symptom was one line; the real lesson was the whole class of bug goes away if you use `readonly record struct` for VOs."
- *"How would you swap the UI for Server-Side Blazor or MAUI?"* → "`AddCookingTime(...)` is the only host-aware call. WASM, SSR, MAUI — each is just another entry point calling the same extension."

---

## Pitch 3 — Architect / principal engineer (90 s, slow down)

> "CookingTime isn't a product story, it's a workflow story. I wanted a non-trivial
> exercise in **using an AI coding agent on a real .NET codebase without producing
> tech debt**. So I picked a 20-year-old WinForms app I had — ~700 lines in one
> file, six real bugs still in production code — and laid down a contract with myself
> before I started.
>
> "The contract was: **the durable artifact lives in the repo, not in the model.** So
> `AGENTS.md` is the standing rules. Six append-only ADRs record every architectural
> decision — target framework, branch model, docs-as-code, SOLID expectations, test-host
> naming, coverage tooling. A locked master plan in `docs/plan.md` and one
> `docs/phases/phase-N-*.md` per phase that lists the verification numbers.
>
> "The migration shipped in eight phases — each one its own branch, one `checkpoint` commit,
> one tag. Eight zero-rebase merges. Each phase landed with its tests: 118 facts total,
> xUnit + Moq + FluentAssertions for the domain, bUnit for the Razor components,
> `WebApplicationFactory<Marker>` for the HTTP integration. The CI workflow runs on
> every PR: build with warnings-as-errors, test with coverlet, then a pure-Python
> coverage gate that blocks any drop below 75 % line coverage on
> `CookingTime.UnitTests`. Measured 80.00 % today.
>
> "What I learned is that the recipe for AI-assisted engineering on a real codebase is
> unglamorous: **read `AGENTS.md` first, respect the layering, ship docs with code,
> let CI be the only definition of done.** The model changes every quarter; that
> workflow doesn't."

**Why this works for architects:** they care about the workflow, not the product. You
lead with "workflow story" and end with the recipe in one sentence. The product
mention is the *hook*, not the topic.

**Likely follow-ups and snappy answers:**

- *"How do you prevent AGENTS.md from going stale?"* → "Six ADRs and per-phase docs. If a layer rule changes, that's an ADR; it doesn't get edited into `AGENTS.md` silently."
- *"How do you onboard a human into the same workflow?"* → "`docs/onboarding.md` — 30-minute read with a code-reading order. Same workflow as the agent, different reader."
- *"Did the AI ever silently violate a constraint?"* → "Once — it tried to add `Microsoft.AspNetCore.Mvc.Testing` to the *production* csproj. The `dotnet build` warning-as-errors caught it because the dependency would have pulled transitive runtime baggage into the WASM payload."
- *"Why no Llama / no GPT comparison?"* → "Model is a deployment detail. The repo's win is that it works on whatever model you plug into the same workflow."

---

## Cheat sheet — three CTAs

| Format | Open with | Close with |
| --- | --- | --- |
| **Recruiter screen** | A hobby project | "Want the GitHub link?" |
| **Senior panel** | Six concrete bugs | "What part would you like me to dig into?" |
| **Architect / principal** | "It's a workflow story" | "The model changes; the workflow doesn't." |

---

## Hand-to-mouth gap-fillers (in case they say "tell me more")

- "Want me to walk you through one of the six bugs?" — bug #1 (mutable VO) and bug #2 (kg/lb) play well on a whiteboard.
- "Want to see the layered diagram?" — [`docs/DIAGRAMS.md`](DIAGRAMS.md) has the Mermaid source; [`docs/diagrams/01-architecture.png`](diagrams/01-architecture.png) is the rendered PNG ready to drop in a chat.
- "Want to see the live CI?" — `phase/7-ci` branch.
- "Want to see the AI workflow?" — [`docs/diagrams/05-ai-workflow.png`](diagrams/05-ai-workflow.png) renders the story; `AGENTS.md` + [ADR 0002](decisions/0002-branch-per-phase.md) capture the rules behind it.

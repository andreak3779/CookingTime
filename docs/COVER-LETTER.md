# CookingTime — Cover-Letter Paragraphs

> Drop-in paragraphs for a cover letter when applying to a **senior / staff / principal
> .NET role**, an **AI-assisted-engineering** role, or a **modernization /
> platform-engineering** team. Each paragraph is calibrated to one persona. Pick the
> one that matches the role, or stack two for an intersection (e.g. senior .NET +
> AI workflow).

> **Usage rule:** Open the letter with a person-specific paragraph (this is *not* it).
> Place the CookingTime paragraph as the **second or third** paragraph of the body,
> immediately before the closing paragraph. Do **not** open with it — it reads as
> bragging unless the reader already knows who you are.

---

## Opening line — pick or adapt

> "Last quarter, I modernized a twenty-year-old WinForms calculator I had lying around
> — a ~700-line single file with six real bugs still in production — into a .NET 10
> Blazor WebAssembly application across eight disciplined phases: layered architecture
> (Domain / Application / Infrastructure / UI), `readonly record struct` value objects,
> Strategy + Factory + Registry + Result-with-validation patterns, and a blocking
> GitHub Actions gate with a Python coverage floor at 75 % on `CookingTime.UnitTests`."

**58 words. Leads with concrete product, ends with concrete numbers. Adjust "Last
quarter" to fit your timeline.**

---

## Variant A — Senior .NET / platform engineering role

> "To demonstrate how I work on a real codebase — not just talk about it — I spent the
> last several months modernizing a side project I owned: a 20-year-old WinForms
> calculator rebuilt on ASP.NET Core Blazor WebAssembly targeting `net10.0`. I shipped
> the migration in eight phases, each one its own branch, one `checkpoint` commit,
> one tag, and one per-phase doc that records what landed and how it was verified.
> Today the build is `dotnet build -c Release` with **0 warnings, 0 errors** under
> `TreatWarningsAsErrors`, **118 automated tests** across xUnit (domain), bUnit (Razor
> components), and `WebApplicationFactory<Marker>` (HTTP integration), and a Python
> coverage gate in GitHub Actions that blocks any PR that drops
> `CookingTime.UnitTests` line coverage on production code below **75 %**. What I'm
> proudest of isn't the numbers — it's that every architectural decision lives in an
> append-only ADR (`docs/decisions/0001..0006`) and there's an `AGENTS.md` (which
> doubles as a contributor guide for new humans) so the next person picking up the
> project gets the same ground truth I had. I bring that pattern to your team: clean
> architecture, tests that ship with code, CI as the only definition of done, and
> decisions that live where future readers look."

**~190 words. Best for: senior backend, platform engineer, staff engineer at a
.NET shop. Use when the role emphasizes "own the system", "raise the bar", or
"make the next engineer's job easier".**

**Filler / condense option:**

> "I modernized a WinForms calculator I own on the side — eight disciplined phases,
> layered architecture, 118 tests, a 75 % coverage gate in CI, and six append-only ADRs
> so the next maintainer (or the next AI agent) has the same ground truth I did. That
> same discipline is what I'd bring to *your* codebase."

---

## Variant B — AI-assisted engineering / "AI-first developer" role

> "I've spent the last several months testing one hypothesis: **can an AI coding agent
> ship a non-trivial .NET codebase without producing tech debt?** I picked a real
> artifact — a 20-year-old WinForms cooking calculator, ~700 lines in one file, six
> known bugs still in production — and laid down a contract with myself before
> starting. The contract was that **the durable artifact lives in the repo, not in
> the model's hidden memory.** So `AGENTS.md` is read first; six append-only ADRs
> record every architectural decision (target framework, branch model, docs-as-code,
> SOLID expectations, test-host naming, coverage tooling); the master plan in
> `docs/plan.md` is locked, and one `docs/phases/phase-N-*.md` per phase records what
> shipped and how it was verified. Eight phases shipped: each one its own branch,
> one `checkpoint(phase-N):` commit, one tag, one verification snapshot. The build is
> zero warnings under `TreatWarningsAsErrors`; 118 automated tests across xUnit, bUnit,
> and `WebApplicationFactory`; a Python gate fails the build if
> `CookingTime.UnitTests` line coverage on production code drops below 75 %
> (currently 80.00 %). The GitHub repo is the working artifact of what I think the
> recipe looks like: ground truth in the repo, agent reads `AGENTS.md`, respects the
> layering, ships docs with code, and CI is the only definition of done. The model
> changes every quarter; the workflow doesn't."

**~260 words. Best for: AI-first dev-tooling roles, "AI engineer" postings, AI-platform
teams. The role explicitly cares about *process*, not product.**

**Filler / condense option:**

> "I tested whether an AI coding agent can ship a real .NET codebase cleanly. The
> recipe that worked: `AGENTS.md` + append-only ADRs + a locked master plan + branch +
> checkpoint + CI-as-only-definition-of-done. Eight phases, 0 warnings, 118 tests, 80 %
> gated coverage. **The model changes every quarter; the workflow doesn't.**"

---

## Variant C — Modernization / legacy migration role

> "When you inherit a 20-year-old WinForms codebase, you have two options: keep it
> alive or rewrite it. I picked a small, owned codebase of mine — a single ~700-line
> `ComCookingTime.cs` with six real bugs still shipping to users — and rewrote it on
> ASP.NET Core Blazor WebAssembly targeting `net10.0`. The migration is documented,
> not magical: a locked master plan in `docs/plan.md`, eight `phase/N-*` branches
> (each with a `checkpoint` commit and a `v0.N-*` tag), six append-only ADRs, and
> per-phase docs that record what shipped and how it was verified. The legacy code
> is preserved untouched under `OriginalSource/` for provenance. Six concrete bugs
> are gone in the new code: a mutable value object whose setter wrote to the wrong
> field, an inverted kg/lb unit conversion, a copy-pasted meal label, three
> half-built Range meals, concrete dependencies in the form constructor, and a
> switch-on-string factory. The new code is layered — `Domain` references nothing,
> `Application` references `Domain`, `Infrastructure` references both, UI references
> all three — with `readonly record struct` value objects, a `MealTypeRegistry`
> that's open-for-extension (new meals register, they never edit a switch), and a
> single composition root (`AddCookingTime`) consumed by both the WASM client and the
> integration test host. The CI gate runs on every PR: build with
> `TreatWarningsAsErrors`, test with coverlet, and a Python script that fails any
> drop in `CookingTime.UnitTests` line coverage below 75 %. Currently **80.00 %**,
> **118 automated tests** across xUnit + bUnit + `WebApplicationFactory`, and
> **0 warnings, 0 errors** on `dotnet build -c Release`. I'd bring the same
> discipline to *your* legacy codebase — modernize in small, reviewable phases; let
> CI enforce the standard; let ADRs record why so the next maintainer doesn't have
> to guess."

**~330 words. Best for: modernization, "rescue the legacy system", platform teams with
a brown-field mandate. The role explicitly cares about *evidence of past rewrites*.**

**Filler / condense option:**

> "When you inherit a 700-line WinForms file with six real bugs, you modernize in
> small phases with a checkpoint per phase, an ADR per decision, a coverage gate in
> CI, and the legacy code kept under `OriginalSource/` for provenance. I shipped
> exactly that — eight phases, 0 warnings, 118 tests, 80 % gated coverage. The repo
> is the receipt."

---

## Variant D — Junior / mid-level .NET role (subordinates you well to context)

> "I built a small but complete .NET project end-to-end on the side: an ASP.NET Core
> Blazor WebAssembly cooking-time calculator on `net10.0`, with a layered architecture
> (Domain / Application / Infrastructure / UI), `readonly record struct` value
> objects, and 118 automated tests across xUnit, bUnit, and `WebApplicationFactory`.
> What I'm proudest of is the operational discipline: every architectural decision
> lives in an append-only ADR (`docs/decisions/0001..0006`), there's an `AGENTS.md`
> mirroring the SOLID + patterns contract so the next contributor (human or AI) has
> the same ground truth, and a Python coverage gate in GitHub Actions blocks any PR
> below 75 % line coverage on `CookingTime.UnitTests`. Today the build is
> `dotnet build -c Release` with 0 warnings, 0 errors, 80.00 % measured coverage.
> The repo reads as a worked example of how I'd like to write software on a team:
> clean architecture, tests that ship with code, and CI as the only definition of
> done."

**~150 words. Best for: mid-level positions where you want to say "I do the
discipline you'd expect, here are the receipts".**

---

## Closing paragraph — pick or adapt

> "I'd love to walk you through any of the six legacy bugs in detail, or through the
> CI workflow that lets a coding agent work on a real codebase without producing tech
> debt — the repo is the artifact. Happy to send the GitHub link or do a 30-minute
> walkthrough on the call."

**35 words. Always pair with a specific offer ("walk through…", "30-minute demo").
A cover letter that ends on a concrete next step converts better than one that ends
on a generic "I look forward to hearing from you".**

---

## Anti-patterns to avoid in the cover letter itself

- **Don't open with the project.** A cover letter opens with the *role*, the
  *company*, or the *team* — never the project. Lead with a sentence like
  "Your job description emphasises X; here's the most relevant thing I've done…"
  and *then* place the CookingTime paragraph.
- **Don't list 12 technologies.** Pick 5 concrete artifacts that prove depth
  (`readonly record struct`, `WebApplicationFactory<Marker>`, `TreatWarningsAsErrors`,
  append-only ADRs, Python coverage gate). The 12-bullet technology sidebar belongs
  on the resume, not the letter.
- **Don't claim AI-experience you don't have.** If your actual day-to-day AI usage is
  one PR a month, say "I use an AI coding agent on side projects to extend the
  discipline I learned on a team codebase" — not "I lead AI-first development".
  Hiring managers verify.
- **Don't quote coverage numbers without the threshold.** Saying "80 % coverage" is
  weaker than "80 % gated at 75 % in CI". The threshold is the proof.
- **Don't over-editorialize.** Pick three facts (e.g. eight phases, 118 tests, one
  CI gate) and let them speak. Editorial superlatives ("I obsessively engineered
  every detail…") undercut the work.

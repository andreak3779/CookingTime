# AGENTS.md — Standing rules for AI agents working in this repo

> Read this first. Every AI agent operating in this repository must follow
> these rules on every task — no need to be reminded. Mirror of [ADR 0004](docs/decisions/0004-solid-and-patterns.md)
> with project-specific implementation conventions layered on top.

## Project at a glance

- **Target framework:** `net10.0` — see [ADR 0001](docs/decisions/0001-target-net10.md).
- **App type:** ASP.NET Core Blazor WebAssembly.
- **Branch + commit model:** see [ADR 0002](docs/decisions/0002-branch-per-phase.md). One feature branch per phase; checkpoint commit at the end.
- **Docs live in-repo:** see [ADR 0003](docs/decisions/0003-docs-as-code.md). Master plan is `docs/plan.md`; per-phase truth is `docs/phases/phase-N-*.md`; ADRs are append-only.
- **Accepted risks are documented:** see [ADR 0009](docs/decisions/0009-accepted-security-risks.md) — no auth, no crypto, no SSRF surface, public client config, no raw-HTML sink. These are decisions, not gaps; do not "fix" them without the revisit condition.
- **Active phase:** see `docs/phases/README.md` (status table).

## Architecture layering (must respect)

```
Domain/         → no project dependencies
Application/    → references Domain only
Infrastructure/ → references Domain + Application
Components/     → references all three (Razor UI)
wwwroot/        → static assets only
tests/          → references what it tests
```

A class in `Domain/` must never `using` from `Infrastructure/` or `Application/`. UI must not construct concretes — use DI.

## SOLID (non-negotiable)

| Letter | Rule |
| --- | --- |
| S | One reason to change per module. Split ComCookingTime-style "kitchen-sink" files. |
| O | Open for extension, closed for modification. New meal types **register**, not edit a switch. |
| L | Subclasses substitutable for their base. No `null`-returning virtuals. |
| I | Small, role-specific interfaces. Split `IRequiredTime`-style god interfaces. |
| D | UI depends on abstractions. DI wires concretes. |

## Design patterns (use appropriately)

- **Strategy** — cooking-time algorithms (`ICookingStrategy`).
- **Factory + Registry** — meal creation (`IMealFactory` + `MealTypeRegistry`).
- **Value Object** — `Weight`, `CookingDuration` (immutable `readonly record struct`).
- **Adapter** — translating UI input → domain units.
- **Template Method / Mediator** — only when justified by domain, never by analogy.

## C# / .NET conventions

- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- File-scoped namespaces (no block-scoped `namespace ... { }`).
- `readonly record struct` for value objects.
- `sealed` on classes not designed for inheritance.
- `nameof()` over string literals.
- No `*.designer.cs`, no `*.resx` for WinForms (this is a Blazor project).
- No `using` legacy types from `OriginalSource/` in new code — `OriginalSource/` is reference only.

## Testing

- **xUnit + Moq + FluentAssertions** baseline.
- **bUnit** for Razor components (Phase 4+).
- **WebApplicationFactory** for integration tests (Phase 5+).
- Tests ship with code, not after.
- Naming: `<Method>_<Scenario>_<Expected>`. Arrange-Act-Assert structure.
- Mock service boundaries; use real objects for value semantics.

## Documentation

- Every phase produces `docs/phases/phase-N-*.md`.
- Every PR updates `README.md` (status table, run-locally).
- ADRs are append-only; supersede with a new entry.

## CI

- PRs into `upgrading-to-net-10` must pass the workflow in `.github/workflows/ci.yml` (full gate lands in Phase 7).
- Locally: `dotnet build -c Release && dotnet test` must be green before pushing.

## Anti-patterns (do NOT reintroduce)

| Pattern | Source in legacy code | Why it bit the original project |
| --- | --- | --- |
| Mutable value object | `OriginalSource/clsTime.MinimumTime` setter writes to `m_MaxTime` | Caused silent bugs in MealBasic/MealRange. |
| Switch-on-string in factories | `OriginalSource/MealCreator.CreateMeal` if/else chain | Adding a meal required editing the factory. |
| Concrete dependencies in UI | `frmCookingTime.m_MealCreator = new CT.MealCreator()` | Untestable. |
| Domain + infrastructure + UI in one file | `OriginalSource/ComCookingTime.cs` | 700-line file mixing 4 concerns. |
| `using` legacy `OriginalSource/` types | future risk | Backward dependency; defeats the upgrade. |
| Reference `.razor` files under the project root | Phase 12 | The Razor SDK compiles **every** `.razor` under the project root unless `EnableDefaultRazorComponentItems=false`. A design/reference copy becomes a real component — and a duplicate route throws `InvalidOperationException` in the browser at router-boot time, while the build stays green and every server-side test passes. See [ADR 0007](docs/decisions/0007-design-package-as-reference-artifact.md). |
| `<RazorComponent Remove="folder\**" />` as a build guard | Phase 12 | Does **not** work against the SDK's default items — they are added *inside* the `ResolveRazorComponentInputs` target, after evaluation-time `Remove` runs. Disable the default items instead. |
| Stray closing tag in a `.js`/`.css` file | Phase 12 | A trailing `</content>` in `wwwroot/js/theme.js` made the browser reject the **whole** script (`Unexpected token '<'`), so `window.cookingTimeTheme` was never defined and the theme toggle threw `Could not find 'cookingTimeTheme.isDark'`. The build stays green. |
| CSS-relative asset URLs resolved against the document | Phase 12 | A CSS `url()` resolves against the **stylesheet's** URL. From `css/cookingtime.css`, `url('fonts/x.woff2')` → `/css/fonts/x.woff2` (404); the fonts live at `/fonts`, so it needs `url('../fonts/x.woff2')`. Assert that the *referencing* document resolves to a served path — asserting only that the asset is served is not enough. |
| A value object that holds a range it never enforces | Phase 14 | `RangeMeal` exposed `MinimumWeight`/`MaximumWeight` and ignored them, so every config-driven meal accepted any weight — a 500 lb roast produced an inverted cook range shown to the user as a *success*. A property that looks like a constraint but isn't one is worse than no property. Enforce it where the knowledge lives. |
| A mock standing in for a domain invariant | Phase 14 | The suite had a test where a **mocked** meal threw `ArgumentOutOfRangeException` — proving the *calculator* handles a throw, never that a real meal throws. Domain invariants need the real type under test. |
| Trusting HTML-encoding to neutralise a URL | Phase 15 | Razor encodes `<`, `>` and `"` in attributes — it does **not** encode a *scheme*. `javascript:alert(1)` contains no encodable character and reaches the browser verbatim as a script URI. Allowlist schemes (`http`/`https`) and render the link only when the value passes. |
| Binding configuration without validating it | Phase 15 | `services.Configure<T>` binds; it does not check. A malformed catalog entry failed at *first render* (an unhandled component exception with no clue which config line was wrong). Use `AddOptions<T>().Bind(...).ValidateOnStart()` with an `IValidateOptions<T>`. |
| Guarding a public file with one pattern | Phase 15 | The served-config test checked only for `@` — an API key or bearer token has no `@` and passed it. Deny secret *shapes* (`sk-`, `ghp_`, `AKIA`, `-----BEGIN`, `password`, …). |
| Publishing the solution | Phase 16 | `dotnet publish` without a project argument copies xunit, Moq, bUnit, coverlet and all three `*Tests.dll` files into the publish root. Always publish `CookingTime.csproj` — and CI now asserts the artifact is clean. |
| Relying on a committed lock file alone | Phase 16 | A `packages.lock.json` is only binding when the restore runs in **locked mode**. Without `--locked-mode`, a restore silently regenerates it and dependency drift is invisible. |
| Setting a property only in the root `Directory.Build.props` | Phase 16 | MSBuild stops at the **nearest** props file, and `tests/Directory.Build.props` shadows the root one — so `RestorePackagesWithLockFile` reached the app project and silently not the three test projects. |
| Mutating a bound model to "reset" a Blazor form | Phase 17 | `EditForm` holds an `EditContext` over the model instance and `DataAnnotationsValidator` caches messages in its `ValidationMessageStore`. Mutating the model does **not** clear that store, so the old messages stayed on screen after Reset. Assign a **fresh** model instance instead. |
| Rebuilding a collection to look one item up | Phase 17 | `CreateAll().FirstOrDefault(...)` re-invoked every factory delegate on **every** calculation. If the factory already holds the collection, give it a lookup method. |

## What you'll see in this repo

- `docs/plan.md` — locked master plan.
- `docs/phases/phase-N-*.md` — what each phase shipped.
- `docs/decisions/000N-*.md` — ADRs.
- `OriginalSource/` — historical WinForms code, do not reference.
- `docs/design/` — reference design packages (Phase 12+). **Snapshot, not a live source**: the app is updated by copying files out of here, so a change here does not change the app until someone re-applies it. Safe to keep `.razor` files here **only** because `EnableDefaultRazorComponentItems` is `false` — see [ADR 0007](docs/decisions/0007-design-package-as-reference-artifact.md).
- `obj/`, `bin/`, `*.csproj.user`, `.vs/` — gitignored build artifacts.

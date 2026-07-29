# AGENTS.md — Standing rules for AI agents working in this repo

> Read this first. Every AI agent operating in `/home/andreak/src/CookingTime` must follow
> these rules on every task — no need to be reminded. Mirror of [ADR 0004](docs/decisions/0004-solid-and-patterns.md)
> with project-specific implementation conventions layered on top.

## Project at a glance

- **Target framework:** `net10.0` — see [ADR 0001](docs/decisions/0001-target-net10.md).
- **App type:** ASP.NET Core Blazor WebAssembly.
- **Branch + commit model:** see [ADR 0002](docs/decisions/0002-branch-per-phase.md). One feature branch per phase; checkpoint commit at the end.
- **Docs live in-repo:** see [ADR 0003](docs/decisions/0003-docs-as-code.md). Master plan is `docs/plan.md`; per-phase truth is `docs/phases/phase-N-*.md`; ADRs are append-only.
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
- No `using` legacy types from `Backup/` in new code — `Backup/` is reference only.

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
| Mutable value object | `Backup/clsTime.MinimumTime` setter writes to `m_MaxTime` | Caused silent bugs in MealBasic/MealRange. |
| Switch-on-string in factories | `Backup/MealCreator.CreateMeal` if/else chain | Adding a meal required editing the factory. |
| Concrete dependencies in UI | `frmCookingTime.m_MealCreator = new CT.MealCreator()` | Untestable. |
| Domain + infrastructure + UI in one file | `Backup/ComCookingTime.cs` | 700-line file mixing 4 concerns. |
| `using` legacy `Backup/` types | future risk | Backward dependency; defeats the upgrade. |

## What you'll see in this repo

- `docs/plan.md` — locked master plan.
- `docs/phases/phase-N-*.md` — what each phase shipped.
- `docs/decisions/000N-*.md` — ADRs.
- `Backup/` — historical WinForms code, do not reference.
- `obj/`, `bin/`, `*.csproj.user`, `.vs/` — gitignored build artifacts.

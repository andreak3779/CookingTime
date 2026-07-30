# ADR 0004 — SOLID + design-patterns expectations

- **Status:** Accepted (Phase 0)

## Context

The legacy `Backup/ComCookingTime.cs` is a single file containing domain, infrastructure, and UI-adjacent code. It contains several SOLID violations and known bugs. We need a clear, written contract of the standards the upgraded code must hold to.

## Decision

The upgraded code must adhere to:

- **S — Single responsibility.** One reason to change per module. No kitchen-sink files.
- **O — Open/closed.** New meal types register, they don't edit a switch statement. New strategies register, they don't edit a base class.
- **L — Liskov substitution.** Subclasses are substitutable for their base. No `null`-returning virtuals.
- **I — Interface segregation.** Prefer small, role-specific interfaces (`ICookingStrategy`, `IMealFactory` — not a god interface).
- **D — Dependency inversion.** UI depends on abstractions. The DI container wires concretes.

Design patterns used appropriately (not decoratively):

- **Strategy** — cooking algorithms.
- **Factory + Registry** — meal creation with open-for-extension requirement.
- **Value Object** — `Weight`, `CookingDuration` (immutable; `readonly record struct`).
- **Adapter** — translating UI input → domain units.
- **Template Method / Mediator** — only when justified by domain, never by analogy.

Implementation conventions:

- `.NET 10` with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- `TreatWarningsAsErrors=true`.
- File-scoped namespaces. `readonly record struct` for value objects. `sealed` by default. `nameof()` over string literals.

## Consequences

**Easier**

- Every future agent working in the repo has a written contract.
- Reviewers can quote this ADR in PR comments.

**Harder**

- More up-front thought; less "just make it work" code.
- Refactoring legacy code costs time up front (amortized across phases).

## Notes

- This ADR is mirrored (and may be augmented by) the repo-root `AGENTS.md` once that file is authored outside the phase branches.

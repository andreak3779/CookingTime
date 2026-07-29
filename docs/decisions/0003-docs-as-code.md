# ADR 0003 — Docs as code

- **Status:** Accepted (Phase 0)

## Context

Two kinds of "plan" documents exist for this project:

1. **Human-facing plans** — read by future humans and future agents to understand the upgrade.
2. **Agent scratchpad** — running notes taken by the planning agent during a session.

Mixing them creates noise.

## Decision

- **Human-facing plans live in the repo:** `docs/plan.md` (locked), `docs/phases/phase-N-*.md` (per-phase truth), `docs/decisions/000N-*.md` (ADRs).
- **Agent scratchpad lives outside the repo** in agent memory (e.g., `/memories/session/`).
- `docs/plan.md` is locked at the end of Phase 0 and evolves only via superseding per-phase docs.
- Per-phase docs evolve each phase.
- ADRs are append-only.

## Consequences

**Easier**

- Newcomers find one place for the architectural story.
- Agent scratchpad can be wiped freely without loss.
- The plan-as-ADR pattern makes later edits traceable.

**Harder**

- Every phase PR carries a docs deliverable (slight overhead).
- Two sources of truth (master plan vs. phase doc) — discipline required.

## Notes

- A periodic review ("are these two still in sync?") is good hygiene but not formalized here.

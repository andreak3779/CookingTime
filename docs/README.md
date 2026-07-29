# CookingTime — Documentation

This directory is the canonical home for project documentation. Code lives in the repo root (and will move into `src/` in Phase 1); tests live under `tests/`; design docs live here.

## Contents

| Folder | What's inside |
| --- | --- |
| [`plan.md`](plan.md) | Master upgrade plan. Locked after Phase 0. The single source of truth for *what we're doing*. |
| [`phases/`](phases/README.md) | One file per phase (Phase 0 → Phase 7). Tracks what each phase shipped and how to verify it. Updated every phase. |
| [`decisions/`](decisions/README.md) | Architecture Decision Records (ADRs). One file per material decision. Append-only. |

## Workflow

- New code is always delivered as a phase branch off the previous merged checkpoint.
- Each phase PR must include a phase doc, a `README.md` delta, and all tests for the code it introduces.
- Material decisions are recorded as new ADRs (never overwrite an old one — supersede with a new entry that links back).

## Status

See [`phases/README.md`](phases/README.md) for the live phase status table.

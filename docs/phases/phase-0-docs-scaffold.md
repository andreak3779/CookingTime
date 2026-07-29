# Phase 0 — Docs Scaffold

| | |
| --- | --- |
| **Branch** | `phase/0-docs-scaffold` |
| **Cuts from** | `upgrading-to-net-10` (renamed from `upgrading-to-net-9`; see ADR 0001) |
| **Checkpoint** | `checkpoint(phase-0): docs scaffold and project plan` |
| **Tag** | `v0.0-docs-scaffold` |
| **Production code** | None |
| **Tests** | None |
| **Docs** | This file; master `docs/plan.md`; ADR 0001–0004; README delta |

## Goal

Establish the `docs/` directory as the canonical home for project documentation, ship the master upgrade plan, and seed the ADR index — all *before* any production code lands. Every subsequent phase reuses this scaffold.

## Why this phase exists

- Without a stable docs skeleton, every phase PR would invent its own structure.
- ADRs are cheaper to start early — the locked-in `.NET 10` and branch conventions are exactly the kind of decisions that benefit from being recorded.
- A locked master plan reduces repetition in every later phase doc.

## Files added

- `docs/README.md` — docs landing page.
- `docs/plan.md` — master upgrade plan (locked at end of this phase).
- `docs/phases/README.md` — phase status table; updated every phase.
- `docs/phases/phase-0-docs-scaffold.md` — this file.
- `docs/decisions/README.md` — ADR index.
- `docs/decisions/0001-target-net10.md`
- `docs/decisions/0002-branch-per-phase.md`
- `docs/decisions/0003-docs-as-code.md`
- `docs/decisions/0004-solid-and-patterns.md`
- `README.md` — repo-root readme with status table and pointer to `docs/`.

## Files modified

- `README.md` — created (none existed).

## Decisions recorded in this phase

- **ADR 0001 — target = .NET 10.** The local SDK is 10.0.110; aligning the TFM removes drift. Renames branch `upgrading-to-net-9` → `upgrading-to-net-10`.
- **ADR 0002 — branch-per-phase with checkpoint commits.** Each phase ships on `phase/<n>-<slug>` with a single `checkpoint(phase-N): …` commit at the end.
- **ADR 0003 — docs as code.** Master plan in `docs/plan.md` (locked); per-phase in `docs/phases/`; agent scratchpad outside the repo.
- **ADR 0004 — SOLID + design-patterns expectations.** Mirrors what will eventually live in `AGENTS.md` once that file is authored.

## Verification

1. `ls -R docs/` shows the expected structure (README, plan, phases/, decisions/).
2. Each file renders correctly when previewed in GitHub (or via local markdown viewer).
3. Internal relative links resolve (no broken anchors).
4. README contains "Documentation" section pointing at `docs/`.
5. `docs/phases/README.md` status table marks Phase 0 ✅; all others ⏳.
6. `git log` shows exactly one checkpoint commit.

## Out of scope

- Production code, tests, CI workflow. All of those arrive in subsequent phases.
- Renaming the on-disk folder layout (`src/` vs root) — that's a Phase 1 decision once we rewrite the csproj.

## Follow-ups (applied in this branch as a second commit)

This phase's checkpoint was committed as `b904e8f`. The follow-up commit (`<hash below>`) adds the supporting-but-non-domain files so we ship them now instead of leaking them into later phases:

- **`AGENTS.md`** (repo root) — standing rules for any AI agent working here. Mirror of ADR 0004 plus project-specific conventions (nullable, file-scoped namespaces, sealed, name-of). Any future agent reads this before touching code.
- **`.gitignore`** — standard .NET ignore (`bin/`, `obj/`, `*.csproj.user`, `.vs/`, test/coverage artifacts), Blazor additions (`appsettings.*.Local.json`), and OS noise (`.DS_Store`, `Thumbs.db`). Makes the worktree clean for builds.
- **`.editorconfig`** — utf-8, LF, 4-space indent for C#, file-scoped-namespaces hint matching AGENTS.md.

**Not applied in this branch (deferred):**

- Skills (`modernize-legacy-dotnet`, `phase-execution`) live outside the repo at `/home/andreak/.agents/skills/` and are not part of any phase branch — they belong to the user's agent environment, not this project's git history.

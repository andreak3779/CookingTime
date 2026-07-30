# ADR 0002 — Branch-per-phase with checkpoint commits

- **Status:** Accepted (Phase 0)

## Context

The upgrade is large (8 phases × code + tests + docs × a checkpoint). Without a disciplined branch strategy, merge conflicts and partial states make progress hard to review and rollback hard to manage.

## Decision

- One feature branch per phase: `phase/<n>-<slug>` cut from the previous phase's branch (or the renamed base, for Phase 0).
- Each phase ends with exactly one `checkpoint(phase-N): <description>` commit.
- Each merged checkpoint is tagged `v0.<N>-<slug>` for cheap rollback.

## Consequences

**Easier**

- PRs are reviewable (one phase = one logical change).
- Reverting a phase is a single `git revert` or `git reset` to the tag.
- Tags form a navigable history of completed phases.

**Harder**

- More branches to manage; tagging discipline must be enforced.
- Reviews need to verify that the checkpoint commit is the **only** commit on the phase branch (ideally).

## Notes

- Squash-merge vs. true merge is a repo-setting choice — we don't bind either way here.
- Each phase's doc must reference its branch name and checkpoint commit hash for traceability.

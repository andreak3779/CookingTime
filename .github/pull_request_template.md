## What

<!-- One-paragraph summary of what this PR does. -->

## Phase

<!-- The phase this PR belongs to (e.g., "Phase 7 — CI gate"). See docs/phases/. -->

- Branch: `phase/N-...`
- Cuts from: `upgrading-to-net-10`
- Checkpoint commit: `checkpoint(phase-N): ...`

## Validation

- [ ] `dotnet build -c Release` is green locally
- [ ] `dotnet test -c Release` is green locally (112+ facts)
- [ ] Line coverage on `CookingTime.UnitTests` stays ≥ 75%
- [ ] Phase doc updated (`docs/phases/phase-N-*.md`)
- [ ] README status table updated
- [ ] AGENTS.md still accurate (no new anti-patterns introduced)
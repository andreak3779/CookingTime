# ADR 0001 — Target framework: .NET 10

- **Status:** Accepted (Phase 0)
- **Phase:** Phase 0
- **Deciders:** Project owner

## Context

The local laptop has the **.NET 10 SDK (10.0.110)** installed. The branch `upgrading-to-net-9` was named for an earlier target and its in-progress files (on disk) referenced `System.Resources.Extensions` 10.0.0, which was already a version mismatch against the stated `net9.0`. The branch name and the project no longer agree on a target framework version.

## Decision

- **TFM:** `net10.0`.
- **Branch rename (local):** `upgrading-to-net-9` → `upgrading-to-net-10`. Performed in Phase 0 step "1 branch rename".
- All Phase 1+ package versions resolve via `dotnet add package` against `net10.0` (no hand-picked version numbers from memory).

## Consequences

**Easier**

- Local toolchain matches TFM (no SDK-ahead-of-target warnings).
- One fewer dimension in the eventual CI matrix.
- The branch name reflects reality.

**Harder**

- Some third-party packages may lag; we'll resolve via `dotnet add package` rather than guessing.
- Branch rename is purely local at Phase 0 (the renamed branch was not yet pushed to `origin`); anyone with a clone needs to `git fetch && git branch -m upgrading-to-net-9 upgrading-to-net-10`.

## Notes

- Renaming is cosmetic but the new name reduces future confusion.
- `net10.0` is the current SDK target on this laptop; revisit during the lifetime of the project if a new LTS ships.

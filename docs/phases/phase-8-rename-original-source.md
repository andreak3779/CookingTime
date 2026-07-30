# Phase 8 — Rename `Backup/` to `OriginalSource/`

| | |
| --- | --- |
| **Branch** | `phase/8-rename-original-source` |
| **Cuts from** | `upgrading-to-net-10` (Phase 7 merged) |
| **Checkpoint** | `checkpoint(phase-8): rename Backup/ to OriginalSource/` |
| **Tag** | `v0.8-rename-original-source` (after merge into `upgrading-to-net-10`) |
| **Production code** | None. Pure rename + comment updates. |
| **Tests** | None. **118 facts unchanged** (95 unit + 9 component + 8 integration). |
| **Docs** | This file; updated `docs/phases/README.md` (this commit); this commit also rewrites every doc that mentions `Backup/` so references point at `OriginalSource/`. |
| **CI** | Unchanged. `Directory.Build.props` + `.github/workflows/ci.yml` still gate on build + test + coverage ≥ 75%. UnitTests line coverage on production code: **80.00%** (gate at 75%). |

> **Note on the commit message.** The Phase 8 checkpoint message says
> "Tests: 95 unit + 9 component + 8 integration + 6 new = 118 facts." The
> "+ 6 new" is a copy-paste error from the Phase 7 message — Phase 8 makes
> no test changes, and the math works out to 118 without the "+ 6".
> Total facts before and after this phase: **118** (verified against
> [phase-7-ci.md](phase-7-ci.md)). Future phases should not propagate the
> "6 new" figure.

## Goal

Rename the historical-reference folder from `Backup/` to `OriginalSource/`
so the folder name describes what it actually contains: the **original
.NET Framework 1.0 WinForms source code**, preserved verbatim for reading.
`Backup/` was a holdover from the early migration that conflated two ideas —
"a backup of the legacy code" and "the historical reference" — and the new
name removes that confusion at the point of contact.

The rename is `git mv` plus every textual reference across the repo. No file
contents inside the renamed folder are touched; history follows the move.

## What shipped

### 1. Folder rename (`Backup/` → `OriginalSource/`) via `git mv`

All 12 files move with their history:

| File (now under `OriginalSource/`) | Origin |
| --- | --- |
| `App.ico` | WinForms app icon |
| `AssemblyInfo.cs` | Legacy WinForms assembly metadata |
| `ComCookingTime.cs`, `ComCookingTime.resx` | Legacy 700-line component hosting code |
| `CookingTime.csproj`, `CookingTime.csproj.user` | Legacy project files (untouched, kept for reference) |
| `CookingTime.resx` | Legacy string resources |
| `CookingTime.sln`, `CookingTime.suo` | Legacy solution + IDE state |
| `frmAbout.cs`, `frmAbout.resx` | Legacy About form |
| `frmCookingTime.cs`, `frmCookingTime.resx` | Legacy main form |

`git mv` preserves blame and log; `git log --follow OriginalSource/ComCookingTime.cs` still walks the pre-rename history.

### 2. Reference updates (16 files, 28 references)

The rename touches every reference across the repo. The full inventory from the checkpoint commit message:

| File | Why it referenced `Backup/` |
| --- | --- |
| [AGENTS.md](../../AGENTS.md) | The "no `using` legacy types from `OriginalSource/`" rule, the anti-pattern table, the repo-layout section, the "what you'll see in this repo" inventory. |
| [README.md](../../README.md) | The "Legacy code preserved, untouched" link. |
| [docs/plan.md](../plan.md) | Master-plan goal ("from `OriginalSource/`") + Phase 6 summary. |
| [docs/decisions/0004-solid-and-patterns.md](../decisions/0004-solid-and-patterns.md) | Legacy-file citation in the SOLID contract. |
| [docs/decisions/0006-coverage-tooling.md](../decisions/0006-coverage-tooling.md) | Coverlet runsettings filter description. |
| [docs/phases/phase-1-blazor-host.md](phase-1-blazor-host.md) | Inventory of files kept as historical reference. |
| [docs/phases/phase-2-domain.md](phase-2-domain.md) | Provenance comments for `Weight`, `ChickenMeal`, `TurkeyMeal`. |
| [docs/phases/phase-3-application-and-config.md](phase-3-application-and-config.md) | Bug-fix citation (legacy kg→lb direction inversion). |
| [docs/phases/phase-6-cleanup.md](phase-6-cleanup.md) | Scope, decisions, and anti-patterns. |
| [docs/phases/phase-7-ci.md](phase-7-ci.md) | Coverlet exclude rule and namespace explanation. |
| [Domain/Meals/ChickenMeal.cs](../../Domain/Meals/ChickenMeal.cs) | `///` provenance comment pointing at `OriginalSource/ComCookingTime.cs:MealChicken`. |
| [Domain/Meals/TurkeyMeal.cs](../../Domain/Meals/TurkeyMeal.cs) | `///` provenance comment pointing at `OriginalSource/ComCookingTime.cs:MealTurkey`. |
| [CookingTime.csproj](../../CookingTime.csproj) | The `Enable*Default*Items` workaround comment — now references `OriginalSource/` and credits Phase 8 for the rename. |
| [coverlet.runsettings.xml](../../coverlet.runsettings.xml) | Filter rules + comment. |
| [scripts/check-coverage.py](../../scripts/check-coverage.py) | `PRODUCTION_FOLDERS` comment. |
| [docs/phases/README.md](README.md) | (this file, added in the same branch — see § 4.) |

### 3. `CookingTime.csproj` comment refresh

The Phase 6 csproj workaround comment was updated to:

```xml
<!-- Phase 6 + Phase 8: limit the production compile glob to the four
     folders that ship with the app. The default Blazor SDK glob
     would also pick up tests/**/*.cs and OriginalSource/*.cs.
       - tests/ lives under separate csproj data and includes
         xUnit/FluentAssertions attributes that aren't valid in the
         production assembly.
       - OriginalSource/ is the historical reference project
         (renamed from Backup/ in Phase 8) and is intentionally not
         built. -->
```

Two reasons remain to keep `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` and `<EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>`:

1. **`tests/` is still under the repo root** — the default compile glob would pick up `tests/**/*.cs` and try to compile xUnit/FluentAssertions attributes into the production assembly, breaking the build.
2. **`OriginalSource/` still contains `.cs` and `.resx` files** — the default globs would pick them up. The renamed folder is no exception; the workaround applies identically to it.

### 4. `docs/phases/README.md` status row

The phase status table gains a Phase 8 row, and the per-phase file list gains a link to this doc. (This is the only *new* file under `docs/phases/` from this phase.)

### 5. Coverage gate, CI, tests — all unchanged

| Metric | Before Phase 8 | After Phase 8 |
| --- | --- | --- |
| Build warnings / errors | 0 / 0 | 0 / 0 |
| Test facts (unit + component + integration) | 118 | 118 |
| UnitTests line coverage on production code | 80.00% | 80.00% |
| `.github/workflows/ci.yml` | Blocking gate (Phase 7) | Blocking gate (Phase 7) |

Pure rename + reference updates cannot affect coverage. The 80.00% number is what the gate measures; nothing in this phase moved it.

## What was deliberately **not** done

- **`docs/plan.md` was not updated to add a Phase 8 row to its branch map.** Per [ADR 0003](../decisions/0003-docs-as-code.md), `plan.md` is **locked** at the end of Phase 0 and evolves only via superseding per-phase docs. Adding Phase 8 to `plan.md` would violate that contract. If you want Phase 8 surfaced there, the right move is a Phase 9-style follow-up that explicitly supersedes the relevant `plan.md` line, not a silent edit.
- **`OriginalSource/` file contents were not changed.** The folder is the *original* source — modifying it would defeat the point. Only the folder name and the references to it moved.
- **No `Backup/` symlink, alias, or redirect was added.** The rename is total. Search engines and external links to the old name will 404; that is acceptable for a project that has never advertised the `Backup/` path publicly.

## Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Rename via `git mv` vs delete-and-add | `git mv` | Preserves blame + history. A delete-and-add would lose the chain `OriginalSource/ComCookingTime.cs → Backup/ComCookingTime.cs → original`. |
| One sweeping commit vs N per-area commits | One commit | Easier to revert and read in `git log`; the rename is conceptually one operation that touches 28 references in 16 files. The commit body enumerates every touched file so the reviewer doesn't need to dig. |
| Update `docs/plan.md` to add Phase 8 to the branch map | No | `plan.md` is locked per [ADR 0003](../decisions/0003-docs-as-code.md). The new phase doc is the per-phase truth for Phase 8. |
| Keep `OriginalSource/` contents verbatim | Yes | Same reason as Phase 6 — "do not reference, but you can read." Modifying the original source would defeat its purpose. |
| Add an ADR for the rename | No | The rename is mechanical (a name + reference sweep) with no architectural choice. ADRs are for material decisions; renaming a folder doesn't qualify. If a future phase supersedes this with a different structure, *that* phase can add an ADR. |
| Leave the Phase 8 commit-message "6 new" copy-paste as-is | Documented in the note at the top of this file | The commit is already on `upgrading-to-net-10`; rewriting its message would rewrite history. Documenting it in this phase doc is the durable fix. |

## Validation

- `dotnet build -c Release` — 0 warnings, 0 errors across all 4 projects.
- `dotnet test -c Release` — **118 facts, 0 failed, 0 skipped** (same as Phase 7).
- `grep -R "Backup" --exclude-dir=.git --exclude-dir=bin --exclude-dir=obj .` after the commit — **no matches** other than this doc's "before" reference to the historical name.
- `git status --short` after the changes — only the 28 expected file modifications plus this new phase doc.

## Anti-patterns to avoid

- **Don't re-introduce a `Backup/` folder** as a symlink or copy. Two names for the same historical-reference folder would re-create the confusion this phase deleted. If a *future* phase needs a separate copy of pre-rename history, give it a clearly distinct name and its own phase doc.
- **Don't edit `OriginalSource/*` files** for "cleanup". The folder is the original source — touching its contents would silently mutate historical reference data. The only legitimate edits to anything inside `OriginalSource/` are `git mv` operations that move a file *out* (e.g., a future phase that ports `ComCookingTime.cs` content into a Domain doc).
- **Don't edit `docs/plan.md` to add Phase 8.** It's locked. If you need the master plan to acknowledge Phase 8, write a new ADR (the doc-as-code pattern is *supersede, don't overwrite*) or update the README at the repo root.

## Out of scope

- ADR for the rename (not material; see decision table above).
- Master-plan update (locked; see "What was deliberately not done").
- `OriginalSource/` content audit or normalization.
- A migration guide for external links to the old `Backup/` path.

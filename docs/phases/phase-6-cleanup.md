# Phase 6 — Cleanup

| | |
| --- | --- |
| **Branch** | `phase/6-cleanup` |
| **Cuts from** | `upgrading-to-net-10` (Phase 5 merged) |
| **Checkpoint** | `checkpoint(phase-6): remove legacy WinForms files + ADR 0005 + phase doc` |
| **Tag** | `v0.6-cleanup` (after merge into `upgrading-to-net-10`) |
| **Production code** | Deleted legacy WinForms files; simplified `CookingTime.csproj` |
| **Tests** | No new tests. Existing 112 facts remain green. |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md`; added [ADR 0005](docs/decisions/0005-test-host-entry-point-naming.md) on the previous `phase/6-cleanup` commit |

## Goal

Remove the legacy WinForms artifacts that were deliberately left in the repo so the upgraded code could be reviewed side-by-side with the original. The upgrade is now feature-equivalent; the legacy files are dead weight. Keep `OriginalSource/` intact as historical reference.

## What shipped

### 1. Deleted legacy root files (20 files + 4 empty directories)

| File / directory | Why it was deleted |
| --- | --- |
| `App.ico` | WinForms icon for the legacy app |
| `AssemblyInfo.cs` (root) | Legacy WinForms assembly metadata |
| `clsMealType.cs`, `clsTime.cs`, `structWeight.cs` (root) | Legacy WinForms value types; superseded by `Domain/ValueObjects/Weight` and `ChickenMeal`/`TurkeyMeal` |
| `ComCookingTime.cs`, `ComCookingTime.resx` | Legacy 700-line component hosting code |
| `CookingTime.csproj.user`, `CookingTime.suo` | IDE artifacts |
| `CookingTime.resx` | Legacy string resources |
| `frmAbout.cs`, `frmAbout.resx` | Legacy WinForms About form |
| `frmCookingTime.cs`, `frmCookingTime.resx` | Legacy WinForms main form |
| `meal.xml` | Legacy meal data store; replaced by `appsettings.json` in Phase 3 |
| `UpgradeLog.XML`, `UpgradeLog2.XML` | IDE upgrade history |
| `_UpgradeReport_Files/` | IDE upgrade report assets |
| `Properties/app.manifest` | Legacy WinForms UAC manifest |
| `Data/` (empty) | Legacy meal data folder |
| `Models/` (empty) | Legacy models folder |
| `Shared/` (empty) | Inherited from the WinForms project; superseded by `Layout/` and `Pages/` |

### 2. `CookingTime.csproj` simplified

The original Phase 1 csproj disabled all three default compile/embedded-resource/Razor globs to prevent the SDK from picking up the legacy files at the repo root:

```xml
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
<EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
```

After the cleanup, the legacy files at the root are gone — but two reasons remain to keep the workarounds:

1. **`OriginalSource/` still contains `.resx` files.** The Blazor SDK's default embedded-resource glob would pick up `OriginalSource/ComCookingTime.resx`, `OriginalSource/CookingTime.resx`, `OriginalSource/frmAbout.resx`, `OriginalSource/frmCookingTime.resx`, whose non-string resources require `System.Resources.Extensions` at runtime. Keeping `<EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>` prevents this.
2. **`tests/` lives under the same repo root.** The default compile glob would pick up `tests/**/*.cs` and try to compile xUnit/FluentAssertions attributes into the production assembly, breaking the build. Keeping `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` and the explicit `<Compile Include="...">` lists constrains the production build to `Program.cs`, `Application/`, `Domain/`, `Infrastructure/`.

The `<EnableDefaultRazorGenerateItems>` workaround was dropped: there are no longer any `.razor` files at the root that need to be excluded, and the explicit `<RazorComponent Include="...">` list was already what was picking up the right files.

The new csproj is a third shorter. The comment on each remaining workaround now points to the *actual* reason (OriginalSource/tests coexistence) rather than the legacy-file reason.

### 3. ADR 0005 — test host entry-point naming

Committed at the start of `phase/6-cleanup` (before the file deletions) so the convention is captured even if the cleanup itself is later reverted. See [ADR 0005](../decisions/0005-test-host-entry-point-naming.md) for the durable pattern: `TestHost.Main` (not `Program.Main`) + `<StartupObject>` for any future test-only Web host.

## What was deliberately **not** deleted

- **`OriginalSource/`** — preserved in full as historical reference. Per [AGENTS.md](../../AGENTS.md), "No `using` legacy types from `OriginalSource/` in new code — `OriginalSource/` is reference only." The two `///` comments in `Domain/Meals/TurkeyMeal.cs` and `Domain/Meals/ChickenMeal.cs` that reference `OriginalSource/ComCookingTime.cs` are *provenance* comments, not dependencies; they stay.
- **`bin/`, `obj/`** — already `.gitignore`d. They will not appear in the commit.
- **`.gitignore`** — unchanged. The existing entries (`bin/`, `obj/`, `*.user`, `*.suo`, `*.sln.docstates`, etc.) already cover everything that was deleted.

## Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Single sweeping commit vs N smaller commits | Single commit | Easier to revert and read in `git log`; the deletions are conceptually one operation. |
| Keep `OriginalSource/` | Yes, untouched | The whole point of `OriginalSource/` is "do not reference, but you can read". Gutting it would defeat the purpose. |
| Restore `<EnableDefaultCompileItems>` | No, still needed | `tests/` is under the same repo root; the SDK default would pick up xUnit tests. |
| Restore `<EnableDefaultEmbeddedResourceItems>` | No, still needed | `OriginalSource/*.resx` exists and would be auto-included. |
| Drop `<EnableDefaultRazorGenerateItems>` | Yes, no longer needed | No `.razor` files at the repo root to exclude. |

## Validation

- `dotnet build -c Release` — 0 warnings, 0 errors across all 4 projects.
- `dotnet test -c Release` — 95 unit + 9 component + 8 integration = **112 facts, 0 failed, 0 skipped**.
- `git status --short` after the changes — only the deletions and the csproj simplification.
- `grep -R "TODO" src tests docs/phases/phase-6*` — no new TODOs.

## Anti-patterns to avoid

- **Don't gut `OriginalSource/`** — the comment-only references in `Domain/Meals/*.cs` are provenance, not dependencies. Removing them would break the per-domain "this came from the legacy table X" documentation.
- **Don't re-enable the SDK defaults wholesale** — they will pick up `OriginalSource/` and `tests/`. The two remaining `EnableDefault*Items` workarounds are durable constraints.
- **Don't add a `Shared/` folder** — the WASM project layout puts shared components in `Layout/`, not `Shared/`. The empty `Shared/` dir was a leftover from the WinForms project.

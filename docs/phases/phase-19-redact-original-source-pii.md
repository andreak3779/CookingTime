# Phase 19 — Redact personal data from `OriginalSource/`

| | |
| --- | --- |
| **Branch** | `phase/19-redact-original-source-pii` |
| **Cuts from** | `main` (`244ee21`) |
| **Checkpoint** | `checkpoint(phase-19): redact personal data from OriginalSource + delete the binary .suo + ADR 0010 + guard test + phase doc` |
| **Tag** | `v0.19-redact-original-source-pii` (after merge) |
| **Production code** | `OriginalSource/frmAbout.cs` (email address + personal homepage emptied), `OriginalSource/AssemblyInfo.cs` (`AssemblyCompany` first name emptied); `OriginalSource/CookingTime.suo` **deleted** |
| **Tests** | New `tests/CookingTime.UnitTests/PersonalDataLeakageTests.cs` (6 facts). **UnitTests total: 199 facts** (was 193) |
| **Docs** | This file; new [ADR 0010](../decisions/0010-redact-pii-in-original-source.md); updated `docs/decisions/README.md`; updated `docs/plan-remove-pii.md`; updated `docs/phases/README.md`; updated `README.md`; updated `docs/plan-codebase-review.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 199 facts green |

## Goal

Remove the personal data that `OriginalSource/` still publishes, without
destroying the folder's value as historical reference material — and close the
gap in the audit that let one item survive in the first place.

## What shipped

### 1. Four pieces of personal data, not two

ADR 0008 decision 7 required `OriginalSource/` to stay verbatim, and ADR 0009
§6 recorded the third-party address as an **accepted risk**. That decision was
made on a partial view of the data. A fresh sweep of the folder found four
items:

| # | Location | Content |
| --- | --- | --- |
| 1 | `OriginalSource/frmAbout.cs` | a third party's email address |
| 2 | `OriginalSource/frmAbout.cs` | a third party's personal homepage URL |
| 3 | `OriginalSource/AssemblyInfo.cs` | a personal first name as `AssemblyCompany` |
| 4 | `OriginalSource/CookingTime.suo` | **a Windows user-profile path** |

Item 4 is the one that matters, because **two audits, a security review, and a
full OWASP pass had already missed it**.

### 2. Why the `.suo` file survived every earlier sweep

[`docs/plan-remove-pii.md`](../plan-remove-pii.md) lists `CookingTime.suo`
under "non-findings", and the parenthetical says it was checked with a binary
`strings` sweep. The sweep *did* read the file — but it was **filtered on the
email pattern**, and the file contains no address. It contains a path: the
original developer's home directory under the legacy Windows profile location,
ending in the Visual Studio project folder.

(The path is not quoted here — a document that reproduces the value
republishes it. Recover it from history with
`git show 446989d:Backup/CookingTime.suo` if it is ever needed.)

The value was published in the original commit (`446989d`, 2025-08-03) and
survived every subsequent review, because each one looked for the shape it
expected. This is the repo's own anti-pattern, already in the table: *the
served-config guard only checked for `@` — an API key or bearer token has no
`@` and passed it* (Phase 15).

The generalisation: **a guard that checks one shape proves only that shape is
absent.**

### 3. What changed

| File | Change |
| --- | --- |
| `OriginalSource/frmAbout.cs` | the `mailto:` link target and the homepage link target are now empty strings |
| `OriginalSource/AssemblyInfo.cs` | `AssemblyCompany("")` — the personal first name removed |
| `OriginalSource/CookingTime.suo` | **deleted** (see below) |
| `docs/plan-remove-pii.md` | the literal third-party address removed from all four places it appeared, including the `git filter-repo` replacement list |

**The folder is otherwise untouched.** No legacy logic was changed — the
redaction is scoped to contact data only, so the folder keeps its value as
reference material.

### 4. Why `CookingTime.suo` is deleted rather than edited

It is a **Visual Studio solution-user file**: machine-specific IDE state,
not source. Concretely:

- The repository's own `.gitignore` already excludes `*.suo` **by rule**
  (`.gitignore:44`). The file was committed in 2025 *before* that rule
  existed, and a tracked file is unaffected by an ignore rule — which is why
  it was still in the tree. `git check-ignore --no-index` confirms the rule
  matches.
- No project references it; it is not compiled (`EnableDefaultCompileItems=false`,
  `DefaultItemExcludes`) and not served (it lives outside `wwwroot/`).
- It is **regenerable**: Visual Studio recreates it when the solution is opened.

Editing a binary IDE artifact in place would leave a corrupt file that no tool
can read; deleting it is both cleaner and honest about what it is.

### 5. The documentation was republishing the value it planned to remove

`docs/plan-remove-pii.md` quoted the third-party address **verbatim** in four
places — most pointedly inside the `git filter-repo` replacement list, where
the literal string was needed as a search-and-replace target. So the document
that planned the removal was itself publishing the address. That is now
replaced with descriptive placeholders, per ADR 0008 decision 8's redaction
convention.

The filter-repo procedure stays actionable: the placeholders say where to
recover the strings from (`git show 446989d:Backup/frmAbout.cs`) rather than
quoting them.

### 6. The guard

New `tests/CookingTime.UnitTests/PersonalDataLeakageTests.cs` (6 facts):

| Fact | What it pins |
| --- | --- |
| `NoTrackedFile_ContainsARealEmailAddress` | no real address in any publishable file |
| `NoTrackedFile_ContainsAWindowsUserProfilePath` | no Windows user-profile path — **the shape that was missed twice** |
| `NoTrackedFile_ContainsAPersonalPosixHomePath` | no personal home directory |
| `OriginalSource_DoesNotContainTheBinaryIdeStateFile` | the `.suo` file stays deleted |
| `OriginalSource_AboutForm_CarriesNoContactStrings` | the two link targets stay empty |
| `OriginalSource_AssemblyInfo_CarriesNoCompanyName` | the first name stays out |

Two implementation notes that matter:

- **Binary files are read as bytes, not skipped.** A file with a NUL byte in
  its first 8 KiB is decoded as Latin-1; otherwise as UTF-8. This is what
  catches a leak in a file with no source-like extension — the `.suo` case.
- **One scan, three facts.** The scan walks the whole repository, so it runs
  once through a `Lazy<T>` and the three facts share the result.

Exemptions are explicit and narrow: RFC 2606 example addresses, GitHub's
noreply form, and the repository's own public handle (which ADR 0008 already
treats as out of scope). The CI runner's own `/home/runner/` workspace path is
exempt too — that is the build agent, not a person.

## Deliberately not done

- **No history rewrite.** Published history still contains all four items.
  That remains a deliberate decision under ADR 0008 decision 5: the owner's
  handle makes the address reconstructible, and rewriting invalidates the
  merged PR references. The verified `git filter-repo` procedure remains in
  the plan doc §7-B, ready to run as its own approved task.
- **The folder is not deleted.** Deleting `OriginalSource/` wholesale would
  have removed the personal data too, but it would also throw away the legacy
  reference material that several provenance comments in `Domain/Meals/` point
  at. Redaction keeps both the data out and the reference in.
- **The third-party's data is not resurrected anywhere.** The redaction is
  not reversible from the working tree — the strings exist only in history.

## Validation

```text
dotnet build -c Release   → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → UnitTests: Passed! 199 total
```

- The unit-test count moved from 193 to 199 (6 new facts).
- `git status --short -- '*.lock.json'` → empty: the committed lock files are
  unchanged, so `--locked-mode` restore still binds.
- The new guard fails when it should: before the redaction it reported two
  real leaks in `docs/plan-remove-pii.md` — a placeholder address and the
  owner's home path. Both were fixed rather than allow-listed. It then caught
  the same three shapes again in **this** phase document, which had quoted the
  very values being redacted; those quotes were removed too.

### A pre-existing restore failure, unrelated to this phase

`dotnet restore` failed on a stale local NuGet cache entry for
`Microsoft.NET.ILLink.Tasks.10.0.15` (`error NU1403: Package content hash
validation failed`). This reproduces on the **pristine base commit** with no
local changes, so it is not caused by this phase. Clearing the cached package
directory and re-running `dotnet restore --force-evaluate` resolved it, and
the regenerated lock files were **identical** to the committed ones (verified
with `git status` on `*.lock.json`).

## References

- [ADR 0010](../decisions/0010-redact-pii-in-original-source.md) — the
  standing decision; supersedes ADR 0008 decision 7 and ADR 0009 §6 in part.
- [ADR 0008](../decisions/0008-no-personal-contact-data.md) — the standing
  "no personal contact data in shipped artifacts" policy.
- [ADR 0009](../decisions/0009-accepted-security-risks.md) — §6 recorded the
  third-party address as an accepted risk; that acceptance is withdrawn.
- [../plan-remove-pii.md](../plan-remove-pii.md) — the original audit, with
  its non-findings corrected.
- [phase-8-rename-original-source.md](phase-8-rename-original-source.md) —
  the rule this phase narrows.

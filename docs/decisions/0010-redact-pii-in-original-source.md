# ADR 0010 — Redact personal data in `OriginalSource/` instead of keeping it verbatim

- **Status:** Accepted (Phase 19)
- **Phase:** Phase 19
- **Deciders:** Project owner
- **Supersedes:** [ADR 0008](0008-no-personal-contact-data.md) decision 7 (in part) and [ADR 0009](0009-accepted-security-risks.md) §6 (in part).
  **Superseded by:** decision 5 only, by [ADR 0011](0011-rewrite-history-into-a-new-repository.md).

## Context

ADR 0008 decision 7 required `OriginalSource/` to stay **verbatim**, third-party
contact strings included, on the reasoning that the folder is historical
reference material and that touching it would defeat its purpose. ADR 0009 §6
recorded the resulting third-party address as an **accepted risk** (OWASP A02).

That decision was made on a **partial** view of the data. A fresh sweep of the
folder found four items, not the two the earlier audit recorded:

| # | Location | Content | Earlier audit |
| --- | --- | --- | --- |
| 1 | `frmAbout.cs` | a third party's email address | known |
| 2 | `frmAbout.cs` | a third party's personal homepage URL | known |
| 3 | `AssemblyInfo.cs` | a personal first name as `AssemblyCompany` | known (called "borderline") |
| 4 | `CookingTime.suo` | **a Windows user-profile path** — the original developer's home directory and account name | **missed** |

Item 4 is the important one. The audit that produced
[`docs/plan-remove-pii.md`](../plan-remove-pii.md) swept the binary `.suo` file
with `strings` and reported it as a **non-finding**. The sweep did read the
file; it was simply *filtered on the email pattern*, and the file contains no
address. It contains a path. The value had been published since the original
commit (`446989d`, 2025-08-03) and survived two audits, a security review, and
a full OWASP pass — because each of them looked for the shape they expected.

This is the repo's own anti-pattern, already written down: *a guard that checks
one shape proves only that shape is absent* (the Phase 15 served-config guard
that checked only for `@`).

The owner has now directed that personal data in `OriginalSource/` be removed
or emptied.

## Decision

1. **Redact in place; do not delete the folder.** The third-party email
   address, the personal homepage URL, and the `AssemblyCompany` first name
   become empty strings. The folder keeps every line of legacy logic — which
   is what makes it reference material — and loses only the contact data.
   This replaces ADR 0008 decision 7's blanket verbatim rule with a narrower
   one: **`OriginalSource/` stays verbatim except for personal data.**
2. **Delete `OriginalSource/CookingTime.suo`.** It is a Visual Studio 2005
   solution-user file: machine-specific IDE state, not source. The
   repository's own `.gitignore` already excludes `*.suo` by rule (the file was
   committed before the rule existed, and a tracked file is unaffected by an
   ignore rule). No project references it; it is not compiled and not served.
   It is regenerable, so deleting it loses nothing that cannot be recreated.
3. **Redact the literal strings from the tracked documentation.**
   `docs/plan-remove-pii.md` quoted the third-party address verbatim in four
   places — including inside the `git filter-repo` replacement list — so the
   document that planned the removal was itself republishing the value. Those
   are replaced with descriptive placeholders, per ADR 0008 decision 8's
   redaction convention.
4. **Guard it with a test, not with a convention.** A new
   `PersonalDataLeakageTests` scans every publishable file — binary files
   included — for three shapes: a real email address, a Windows user-profile
   path, and a personal POSIX home path. The file-level facts pin the two
   redacted sources and the deleted artifact.
5. **No history rewrite.** Published history still contains all four items.
   That remains a deliberate decision under ADR 0008 decision 5, for the same
   reason: the owner's handle makes the address reconstructible, and rewriting
   would invalidate the merged PR references. The verified `git filter-repo`
   procedure remains in the plan doc §7-B.

## Consequences

**Easier**

- The third party's contact data and the original developer's local path are no
  longer published in the working tree.
- The folder keeps its value as reference material: no legacy logic was
  touched.
- A future commit that reintroduces personal data fails CI instead of
  publishing it. This is the first guard in the repo that reads binary files,
  so it covers the class of leak that two previous audits missed.
- The audit trail is complete: ADR 0010 supersedes the parts of ADR 0008 and
  ADR 0009 it changes, and both link forward.

**Harder**

- `OriginalSource/` is no longer byte-for-byte identical to the 2005 source.
  That is the cost of the owner's instruction, and it is scoped as narrowly as
  possible: contact data only.
- The guard scans the working tree, so it reads files a contributor may not
  have staged. It is a repository-shape assertion, not a staging-area check —
  the same trade-off `BuildHardeningTests` already makes.
- The Windows user-profile path and the third-party address remain in
  published history until an approved rewrite.

**Neutral**

- RFC 2606 example addresses and GitHub noreply addresses stay; they are not
  personal data.
- The repository's public handle stays — it is the project's identity, and
  ADR 0008 already treats it as out of scope.

## References

- [../plan-remove-pii.md](../plan-remove-pii.md) — the original audit, its
  evidence, and the (now superseded) recommendation to leave the folder
  untouched.
- [0008-no-personal-contact-data.md](0008-no-personal-contact-data.md) — the
  standing "no personal contact data in shipped artifacts" policy; decision 7
  is superseded in part by this ADR.
- [0009-accepted-security-risks.md](0009-accepted-security-risks.md) — §6
  recorded the third-party address as an accepted risk; that acceptance is
  withdrawn by this ADR.
- [../phases/phase-19-redact-original-source-pii.md](../phases/phase-19-redact-original-source-pii.md)
  — per-phase truth.

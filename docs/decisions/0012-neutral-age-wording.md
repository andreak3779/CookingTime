# ADR 0012 — Describe the legacy app by its technology, not its age

- **Status:** Accepted
- **Deciders:** Project owner
- **Amends:** [ADR 0010](0010-redact-pii-in-original-source.md) decision 1
  (`OriginalSource/` stays verbatim except for personal data).

## Context

The README, pitch, cover-letter and slides text described the starting point
as "a 20-year-old WinForms calculator". `OriginalSource/` also carried the year
2005 in three places: the assembly copyright, the About form's third label, and
the solution file's "Visual C# Express 2005" comment. A reviewer should judge
the engineering, and the age of the original app says nothing about it.

## Decision

1. **Docs say "legacy".** "A legacy WinForms calculator" replaces the age
   claim. The technology facts stay: .NET Framework 2.0, WinForms, about 700
   lines in one file, six defects.
2. **Three year strings in `OriginalSource/` are emptied:**
   `AssemblyCopyright`, the About form's third label, and the solution file's
   `# Visual C# Express 2005` comment. No logic changes.
3. **The phase 19 doc, ADR 0010 and one test message** say "Visual Studio"
   instead of "Visual Studio 2005". The meaning is unchanged.

## Consequences

**Easier**

- The documents describe the work, not a date.

**Harder or not fixed**

- `OriginalSource/` is now verbatim except for personal data **and** these
  three year strings.
- The technology is still dateable: .NET Framework 2.0, the solution
  `Format Version 9.00` line and the 2003 MSBuild schema URI remain. They are
  facts about the code and the schema URI cannot change, so they stay.
- This changes the working tree only. Earlier commits in history still contain
  the old text. Rewriting history again was not requested.

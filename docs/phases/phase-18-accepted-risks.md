# Phase 18 — Accepted risks and review close-out

| | |
| --- | --- |
| **Branch** | `phase/18-accepted-risks` |
| **Cuts from** | `main` (which contains Phases 14–17) |
| **Checkpoint** | `checkpoint(phase-18): record accepted security risks in ADR 0009 + close out the review + phase doc` |
| **Tag** | `v0.18-accepted-risks` (after merge) |
| **Production code** | None. This phase changes documentation only. |
| **Tests** | None. No behaviour changed. |
| **Docs** | New [ADR 0009](../decisions/0009-accepted-security-risks.md); updated `docs/decisions/README.md`; updated `docs/phases/README.md`; updated `README.md`; updated `AGENTS.md`; new `docs/plan-codebase-review.md` (the review this whole sequence came from) |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 248 facts green |

## Goal

Phases 14–17 fixed everything the review found that was *fixable*. What
remained was a set of properties that are **absent by design**: no
authentication, no cryptography, no SSRF surface, a publicly served config
file, no raw-HTML sink.

Those absences are easy to mistake for oversights. A future reviewer — human
or agent — would re-report them, and someone would eventually "fix" one by
adding a server-side component that the application has no need for.

This phase writes them down as **decisions**, each with the condition that
would make it worth revisiting.

## What shipped

### ADR 0009 — Accepted security risks for a client-side-only application

The ADR records eight items:

| # | Accepted | Revisit when |
| --- | --- | --- |
| 1 | No authentication or authorization (A07, A01) | The app gains a server, a database, or user data |
| 2 | No cryptography (A02) | The app makes network requests or stores sensitive data |
| 3 | No SSRF surface (A10) | A server-side component fetches URLs |
| 4 | The client config file is public (A05, A04) | A secret ever needs to live in configuration |
| 5 | No raw-HTML sink exists (A03) | Any component renders untrusted markup |
| 6 | `OriginalSource/` keeps a third party's address (A02) | The owner deletes the folder |
| 7 | The clickjacking directive is absent from the meta CSP (A05) | The nginx layer in Phase 9 lands |
| 8 | A `javascript:` URI was reachable from config — now closed (A03) | Any config value reaches an `href`, `src`, or event handler |

Item 5 is the one most likely to be destroyed by accident, so it is stated as
a **property** rather than a gap: any raw-HTML render must come with a
sanitisation decision.

Item 8 is included because it is the **shape** the others could take. It is
closed now (Phase 15), but recording the shape means the next config value
that reaches an `href` is recognisable.

### Every claim was verified, not asserted

An ADR that says "there is no raw-HTML sink" is only worth something if it
was checked. Each claim was re-verified against the repository:

| Claim | Verification |
| --- | --- |
| No raw-HTML sink | Grep for `MarkupString`, `Html.Raw`, `innerHTML`, `outerHTML`, `document.write` across all tracked files — **zero matches** |
| No network calls | Grep for `HttpClient`, `GetAsync`, `PostAsync`, `WebRequest`, `fetch(`, `XMLHttpRequest` in production code — **no matches** |
| No auth or credentials | Grep for `authoriz`, `authenticat`, `password`, `credential`, `bearer`, `apikey` in production code — **no matches** |
| The 4 legacy help pages are inert | All four read in full — no `<script>`, no `on*=` handler, no external resource |
| `OriginalSource/` is not compiled or served | `EnableDefaultCompileItems=false` + `DefaultItemExcludes`; the folder is outside `wwwroot/` |
| The meta CSP ignores the clickjacking directive | Observed in Chromium: *"The Content Security Policy directive 'frame-ancestors' is ignored when delivered via a `<meta>` element"* |

### The review itself is now in-repo

`docs/plan-codebase-review.md` holds the original review: the architecture
reading, the 16 findings with severity and file:line evidence, the
remediation plan, and the notes on what was explicitly checked and found
clean. A review that only exists in a chat log is a review that gets
repeated; this one can be re-run against a later commit and diffed.

## The whole sequence, in one table

| Phase | Findings closed | Facts |
| --- | --- | --- |
| 14 — Domain input validation | C-01 `RangeMeal` accepted any weight · C-02 `CookingDuration` allowed `Min > Max` · C-03 `RangeCookingStrategy` allowed `max < min` | 158 → 170 |
| 15 — Config trust boundary | S-01 `javascript:` URI sink · S-02 no CSP · S-04 no startup validation · S-07 narrow secret guard · S-08 non-strict JSON | 170 → 230 |
| 16 — Build and release hardening | S-03 floating versions, no lock file · S-05 unpinned actions · B-01 test assemblies in the publish root · B-02 version skew | 230 → 241 |
| 17 — UI robustness and cleanup | C-04 `Reset` left stale validation messages · C-05 catalog rebuilt per calculation · B-04 dead API · S-06 no logging | 241 → 248 |
| 18 — Accepted risks | ADR 0009 records the eight deliberate absences | 248 |

### The critical finding, for the record

C-01 was the one that mattered. Every config-driven meal — the entire
shipped catalog of ten roasts and hams — accepted **any** weight, and the
app reported the result as a **success**:

```
Pork Roast @ 500 lb   IsSuccess=True   "…between of 21 hrs. and 20 mins. to 15 hrs. and 0 mins."
```

The *minimum* (21h20m) exceeded the *maximum* (15h00m). `ChickenMeal`
rejected the same input, so the two meal kinds disagreed about what a valid
weight is. Fixed in Phase 14; the guard now lives in `RangeMeal`, where the
knowledge lives.

## Validation

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → UnitTests 191 · ComponentTests 23 · IntegrationTests 34
                             = 248 facts, 0 failed, 0 skipped
coverage gate             → UnitTests 86.01%  (gate 75%)  PASS
```

No production code changed in this phase, so the test counts are unchanged
from Phase 17.

## Anti-patterns to avoid

- **Don't treat a deliberate absence as an oversight.** "No auth" in a
  client-side calculator with no user data is a decision, not a gap. ADR
  0009 records it so the next pass does not re-report it.
- **Don't ship an ADR whose claims were never checked.** Every claim in
  ADR 0009 names the command that verified it. An unverified "there is no X"
  is worse than silence, because it stops the next reader from looking.
- **Don't let a review live only in a chat log.** It cannot be re-run,
  diffed, or audited.

## Out of scope

- Phase 9 (containerisation) now has an enforced publish command and a
  documented place to put the clickjacking and HTTPS headers.
- A future review against a later commit — `docs/plan-codebase-review.md`
  is the baseline to diff against.

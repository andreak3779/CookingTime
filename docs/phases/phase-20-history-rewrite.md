# Phase 20 — Purge personal data from git history

| | |
| --- | --- |
| **Branch** | `phase/20-history-rewrite` |
| **Cuts from** | `main` (`be97e88`) |
| **Checkpoint** | `checkpoint(phase-20): phase doc + README/index updates for the history rewrite (ADR 0011)` |
| **Tag** | `v0.20-history-rewrite` (after merge) |
| **Production code** | None. |
| **Tests** | None added. Existing suite unchanged: UnitTests 199 + ComponentTests 23 + IntegrationTests 34 = 256 facts, all green on the rewritten tree |
| **Docs** | This file; new [ADR 0011](../decisions/0011-rewrite-history-into-a-new-repository.md); updated `docs/decisions/README.md`, `docs/phases/README.md`, `README.md`, `AGENTS.md`; forward links added to ADRs 0008 and 0010 |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 256 facts green |

> **Most of this phase happened outside the repository.** The rewrite itself
> produced a *new repository*, so its result is the repository's first commit
> rather than a diff on `main`. This PR records it: the ADR, this file, and the
> index updates.

## Goal

Remove personal data from **git history**, not just the working tree.
Phases 13 and 19 cleaned the tree and explicitly left history alone (ADR 0008
decision 5, ADR 0010 decision 5). The owner approved the follow-up.

## What shipped

### 1. A rewritten copy, published as a new repository

| | Before | After |
| --- | --- | --- |
| Repository | `andreak3779/CookingTime` (private) | renamed `andreak3779/CookingTime-private-archive`, still private |
| Public repository | — | new `andreak3779/CookingTime` |
| Refs published | every branch and tag, incl. Dependabot branches and agent checkpoint refs | `main` + the 9 phase tags only |
| Commits on `main` | 68 | 68 (tip `f695354` → `be97e88`) |

A force-push over the old repository was rejected. GitHub keeps pull-request
refs and cached commit views that a push does not remove, and the old
repository held the name the new one needed.

### 2. What was removed, and how

Three mechanisms — text replacement, path deletion and a mailmap — because the
data lived in several forms
([ADR 0011](../decisions/0011-rewrite-history-into-a-new-repository.md)):

| Data | Form in history | Mechanism |
| --- | --- | --- |
| Third party's address and homepage URL | text in `frmAbout.cs`; **UTF-16 inside legacy `.exe` files** | text replace; binaries deleted |
| Legacy developer's first name and Windows profile path | `AssemblyInfo.cs`; IDE state, `.pdb`, build output, upgrade logs (ASCII **and** UTF-16) | text replace; binaries and logs deleted |
| Owner's personal address | `appsettings.json`, docs, commit/tag identities | text replace; **mailmap** to the GitHub noreply address (author, committer, tagger) |
| Owner's POSIX home directory | `AGENTS.md` and three docs | text replace → `/home/<user>` |

Paths deleted from all history: root `bin/`, `obj/`, `*.suo`,
`*.csproj.user`, `UpgradeLog*.XML`. None existed at HEAD.

### 3. Why the plan doc's procedure was not enough

`docs/plan-remove-pii.md` §7-B replaced text and rewrote one email. A
byte-level sweep before running it showed the third party's address *and* the
legacy developer's name inside the legacy `.exe` files, as UTF-16 — which a
text replacement does not reliably reach. The binaries had to be deleted
instead. This is the Phase 19 lesson again: **a sweep proves only the shapes
it looked for** — so this one read every blob as raw bytes, in two encodings,
for every token found anywhere.

## Verification

Run against a **fresh clone of the published repository**, not the working
copy:

```text
blob scan (ASCII + UTF-16LE, all history)  → no hits for any leaked token
email-shaped strings, all history          → noreply, example.com, project .local fixture only
commit messages + tag annotations          → no hits
dotnet build -c Release                    → 0 Warning(s), 0 Error(s)
dotnet test  -c Release                    → 199 + 23 + 34 = 256 passed
```

## Consequences to know about

- **Every SHA changed.** SHAs in older phase docs and ADRs (for example
  `244ee21` in Phase 19, `446989d` and `d574e18` in the plan doc) name commits
  in the **archive**. Those documents are append-only and are not edited.
- **No PR or issue history carried over.** Dependabot reopens its PRs.
- **Repository settings did not transfer** (branch protection, secrets, Pages,
  Actions permissions). Checking them is a follow-up, listed below.
- **The archive still holds everything.** Never make it public, and never push
  or merge its branches here. `AGENTS.md` now says so.

## Deliberately not done

- **The owner's given name and handle stay.** They are the project's identity
  (ADR 0008).
- **Past exposure is not recalled.** Copies from any period when the old
  repository was reachable cannot be found or removed from here.
- **Historical docs keep their stale SHAs and "no history rewrite" text.**
  ADR 0011 supersedes the decisions; the earlier ADRs carry forward links.
- **Test count in `README.md` is unchanged** (it says 250; the suite is 256).
  That drift predates this phase, and this PR is about history, not counts.

## Follow-ups (not in this PR)

- Verify branch protection, Actions permissions, Dependabot and Pages
  settings on the new repository.
- Reconcile the README's test count and the repository description with the
  actual 256.

# Master plan status note — Phases 12–18 shipped after this plan was locked

> **Status:** ✅ Accurate as a record of the *original* upgrade (Phases 0–7).
> **Superseded in part** by the phases listed below. Per
> [ADR 0003](../decisions/0003-docs-as-code.md), [`docs/plan.md`](../plan.md)
> is locked and evolves only via superseding per-phase docs — **this file is
> that superseding doc.** The master plan itself was deliberately **not**
> edited.

| | |
| --- | --- |
| **Branch** | `docs/master-plan-status-note` |
| **Purpose** | Point a reader of the locked master plan at the work that landed after it, without editing the locked file |
| **Cuts from** | `main` (contains Phases 14–18) |
| **Docs** | This file; `docs/phases/README.md` (pointer); `README.md` (documentation map) |
| **CI** | Unchanged. Documentation-only — no code touched. |

## Why this file exists

[`docs/plan.md`](../plan.md) was locked at the end of Phase 0 and describes
Phases 0–7. Eight further phases have since shipped. A reader who opens the
master plan today gets a **true but incomplete** picture, and — because the
plan is the document AGENTS.md points to first — the most likely first
impression is that the project is at Phase 7.

That gap is not new. The master plan was already five phases behind before
the codebase review began: it listed neither Phase 7b (the Blazor WASM boot
script fix), Phase 8 (the `OriginalSource/` rename), Phase 12 (the UI theme),
nor Phase 13 (the PII removal), and it still described a `Components/` folder
that does not exist and a `src/CookingTime` project path that was never the
layout.

**This file does not repair those stale lines.** It points at the truth.
Repairing them would mean editing a locked file, which ADR 0003 forbids and
which [Phase 8](phase-8-rename-original-source.md) already set a precedent
against — it recorded the same gap and chose the same answer:

> **Don't edit `docs/plan.md` to add Phase 8.** It's locked. If you need the
> master plan to acknowledge Phase 8, write a new ADR (the doc-as-code
> pattern is *supersede, don't overwrite*) or update the README at the repo
> root.

## What shipped after the plan was locked

| Phase | Branch | What it added | Phase doc |
| --- | --- | --- | --- |
| 7b | `phase/7b-blazor-boot-script` | The WASM boot script referenced `blazor.web.js`, which the .NET 10 SDK does not emit — the payload never bootstrapped | [phase-7b](phase-7b-blazor-boot-script.md) |
| 8 | `phase/8-rename-original-source` | `Backup/` → `OriginalSource/`; the legacy project was also removed from the compile glob | [phase-8](phase-8-rename-original-source.md) |
| 12 | `phase/12-ui-theme` | The `cookingtime-theme` design package: stylesheet, restyled pages, icon set, self-hosted fonts, dark-mode toggle, WCAG fixes. Exposed the duplicate-route bug the design package would have caused | [phase-12](phase-12-ui-theme.md) |
| 13 | `phase/13-remove-email-setting` | Personal contact data removed; **client configuration delivery** fixed — the WASM client had been receiving *no* configuration, leaving the production meal catalog empty | [phase-13](phase-13-remove-email-setting.md) |
| 14 | `phase/14-domain-validation` | Every config-driven meal accepted *any* weight; a 500 lb roast produced an inverted cook range shown as a *success* | [phase-14](phase-14-domain-validation.md) |
| 15 | `phase/15-config-trust` | Config validated at startup; `ResumeUrl` scheme allowlist; Content-Security-Policy; a secret-shape guard on the served config | [phase-15](phase-15-config-trust.md) |
| 16 | `phase/16-build-hardening` | Exact package versions + lock files; SHA-pinned actions; a vulnerability gate; a publish-output check that keeps test assemblies out of the artifact | [phase-16](phase-16-build-hardening.md) |
| 17 | `phase/17-ui-cleanup` | `Reset()` no longer leaves stale validation messages; the meal catalog is built once instead of per calculation; logging; dead API removed | [phase-17](phase-17-ui-cleanup.md) |
| 18 | `phase/18-accepted-risks` | The review's deliberate absences recorded as decisions, each with its revisit condition | [phase-18](phase-18-accepted-risks.md) |

Phases 9–11 (containerise, deploy to a local cluster, expose and verify) are
still **planned** — see [`plan-kubernetes.md`](../plan-kubernetes.md) and the
status table in [`phases/README.md`](README.md).

## Where the truth actually lives

The master plan is a record of the original upgrade, not a live index. For
current state:

| Question | Authoritative source |
| --- | --- |
| What phase is the project on? | [`docs/phases/README.md`](README.md) — the status table |
| What did a given phase do? | The matching `docs/phases/phase-N-*.md` |
| Why is it done that way? | [`docs/decisions/README.md`](../decisions/README.md) — ADRs 0001–0009, append-only |
| How do I run it? | [`README.md`](../README.md) |
| What did the codebase review find? | [`plan-codebase-review.md`](../plan-codebase-review.md) |
| What are the standing rules for an agent? | [`AGENTS.md`](../../AGENTS.md) |

The `docs/phases/README.md` table is the index: it carries every phase, its
branch, its checkpoint commit, and whether it is shipped or planned.

## Current shape of the project

For a reader who arrived via the master plan, these are the places where it
no longer matches the code. **This table is the note** — the master plan's
own lines are left untouched.

| `plan.md` says | Reality | Why it differs |
| --- | --- | --- |
| `Components/` — Razor components / pages | `Layout/` and `Pages/` | The UI folders were named for their role; `Components/` was never created |
| `dotnet run --project src/CookingTime` | `dotnet run` from the repo root | The project has always been at the repo root — the `src/` path was aspirational |
| `appsettings.json` with 13 meals | 12 meals — 10 configured in `wwwroot/appsettings.json` plus 2 code-baked (`Chicken`, `Turkey`) | Phase 3's count included the two code-baked meals; the config file itself carries 10 |
| Branches and tags for Phases 0–7 only | 18 phases, branches `phase/0-*` … `phase/18-*` | Phases 7b–18 landed after the plan was locked |
| *(Current phase.)* on Phase 0 | Phase 18 is the latest shipped phase | The annotation was never updated — it was written when Phase 0 was current |
| No mention of a UI theme, dark mode, or self-hosted fonts | All three shipped in Phase 12 | Post-lock work |
| No mention of a CSP, package lock files, or SHA-pinned actions | All three shipped in Phases 15–16 | Post-lock work |

## Tags

The plan's branch map has a Tag column. Tags exist through **Phase 8**
(`v0.8-rename-original-source`); phases 9–11 are planned; **phases 12–18
have no tags yet**. ADR 0002 asks for one tag per merged phase, so tagging
12–18 is outstanding — it is a release-hygiene task, not a documentation
gap, and it is listed here so the gap is visible rather than assumed.

## What this file deliberately does not do

- **It does not edit `docs/plan.md`.** ADR 0003 locks it; Phase 8 set the
  precedent for leaving it alone. This file is the superseding doc that
  ADR 0003's escape hatch calls for.
- **It does not duplicate the phase docs.** It points at them. If a phase doc
  and this file disagree, the phase doc wins — that is the whole point of the
  layering.
- **It does not claim the master plan is wrong.** It describes the plan as
  accurate for its scope and incomplete for later work.

## Validation

Documentation-only. No code, no tests, no configuration changed.

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → 250 facts, 0 failed
```

## Anti-patterns to avoid

- **Don't edit `docs/plan.md` to add a phase.** It is locked. Write the
  superseding per-phase doc, and — if the master plan needs to acknowledge
  it — say so in the README, which is not locked.
- **Don't let the master plan be the first thing a newcomer reads without a
  pointer.** That is what this file and `docs/phases/README.md` are for.
- **Don't duplicate phase detail here.** A second copy drifts.

## References

- [../plan.md](../plan.md) — the locked master plan this note supersedes in part.
- [../decisions/0003-docs-as-code.md](../decisions/0003-docs-as-code.md) —
  the docs-as-code contract, including the "supersede, don't overwrite" rule.
- [phase-8-rename-original-source.md](phase-8-rename-original-source.md) —
  the precedent: the same gap, recorded the same way.
- [../decisions/0002-branch-per-phase.md](../decisions/0002-branch-per-phase.md)
  — the one-branch-per-phase + one-tag-per-phase convention, and the
  outstanding tagging gap for phases 12–18.

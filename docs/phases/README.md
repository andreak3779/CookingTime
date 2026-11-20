# Phases

One file per phase. Updated as phases ship.

> **Reading this index?** The master plan ([`docs/plan.md`](../plan.md)) was
> locked after Phase 7 and describes that original upgrade. This table is the
> **current** index — every phase, its branch, its checkpoint commit, and
> whether it shipped. For the gap between the two, see
> [Master plan status note](master-plan-status-note.md).

| # | Phase | Branch | Checkpoint | Status |
| --- | --- | --- | --- | --- |
| 0 | Docs scaffold | `phase/0-docs-scaffold` | `checkpoint(phase-0): docs scaffold and project plan` | ✅ Done |
| 1 | Blazor host | `phase/1-blazor-host` | `checkpoint(phase-1): scaffold blazor host + host build test + phase doc` | ✅ Done |
| 2 | Domain layer | `phase/2-domain-layer` | `checkpoint(phase-2): domain layer + unit tests + ci smoke + phase doc` | ✅ Done |
| 3 | Application + config | `phase/3-application-and-config` | `checkpoint(phase-3): application service + unit tests + phase doc` | ✅ Done |
| 4 | Razor UI | `phase/4-razor-ui` | `checkpoint(phase-4): razor UI + component tests (bUnit) + phase doc` | ✅ Done |
| 5 | Integration + feature tests | `phase/5-integration-and-feature-tests` | `checkpoint(phase-5): integration + feature tests + phase doc` | ✅ Done |
| 6 | Cleanup | `phase/6-cleanup` | `checkpoint(phase-6): remove legacy WinForms files + ADR 0005 + phase doc` | ✅ Done |
| 7 | CI gate | `phase/7-ci` | `checkpoint(phase-7): blocking CI gate + coverage + ADR 0006 + phase doc` | ✅ Done |
| 7b | Blazor WASM boot script fix | `phase/7b-blazor-boot-script` | `checkpoint(phase-7b): fix Blazor WASM boot script reference + regression tests + phase doc` | ✅ Done |
| 8 | Rename `Backup/` → `OriginalSource/` | `phase/8-rename-original-source` | `checkpoint(phase-8): rename Backup/ to OriginalSource/` | ✅ Done |
| 9 | Containerize (see [plan-kubernetes.md](../plan-kubernetes.md)) | `phase/9-containerize` | `checkpoint(phase-9): dockerfile + podman/docker build verified + phase doc` | 🔜 Planned |
| 10 | Local Kubernetes deployment | `phase/10-k8s-deploy` | `checkpoint(phase-10): k8s manifests + deployed to local cluster + phase doc` | 🔜 Planned |
| 11 | Expose, verify, document | `phase/11-k8s-docs` | `checkpoint(phase-11): app reachable in-cluster + README + phase doc` | 🔜 Planned |
| 12 | CookingTime UI theme (design implementation) | `phase/12-ui-theme` | `checkpoint(phase-12): apply cookingtime-theme design + dark-mode toggle + phase doc` | ✅ Done |
| 13 | Remove personal contact data + fix client config delivery | `phase/13-remove-email-setting` | `checkpoint(phase-13): remove personal contact data from config/UI + fix client config delivery + ADR 0008 + phase doc` | ✅ Done |
| 14 | Domain input validation | `phase/14-domain-validation` | `checkpoint(phase-14): enforce meal weight ranges + duration ordering invariants + phase doc` | ✅ Done |
| 15 | Config trust boundary | `phase/15-config-trust` | `checkpoint(phase-15): validate config at startup + allowlist ResumeUrl scheme + add CSP + harden served-config guard + phase doc` | ✅ Done |
| 16 | Build and release hardening | `phase/16-build-hardening` | `checkpoint(phase-16): pin package versions + lock files + SHA-pinned actions + publish-leak check + dependabot + phase doc` | ✅ Done |
| 17 | UI robustness and cleanup | `phase/17-ui-cleanup` | `checkpoint(phase-17): fix Reset stale validation state + name-keyed meal lookup + add logging + remove dead API + phase doc` | ✅ Done |
| 18 | Accepted risks and review close-out | `phase/18-accepted-risks` | `checkpoint(phase-18): record accepted security risks in ADR 0009 + close out the review + phase doc` | ✅ Done |
| 19 | Redact personal data from `OriginalSource/` | `phase/19-redact-original-source-pii` | `checkpoint(phase-19): redact personal data from OriginalSource + delete the binary .suo + ADR 0010 + guard test + phase doc` | ✅ Done |

## Per-phase files

- [Phase 0 — Docs scaffold](phase-0-docs-scaffold.md) ✅
- [Phase 1 — Blazor host](phase-1-blazor-host.md) ✅
- [Phase 2 — Domain layer](phase-2-domain.md) ✅
- [Phase 3 — Application + config](phase-3-application-and-config.md) ✅
- [Phase 4 — Razor UI](phase-4-razor-ui.md) ✅
- [Phase 5 — Integration + feature tests](phase-5-integration-and-feature-tests.md) ✅
- [Phase 6 — Cleanup](phase-6-cleanup.md) ✅
- [Phase 7 — CI gate](phase-7-ci.md) ✅
- [Phase 7b — Blazor WASM boot script fix](phase-7b-blazor-boot-script.md) ✅
- [Phase 8 — Rename `Backup/` → `OriginalSource/`](phase-8-rename-original-source.md) ✅
- [Phase 12 — CookingTime UI theme](phase-12-ui-theme.md) ✅
- [Phase 13 — Remove personal contact data + fix client config delivery](phase-13-remove-email-setting.md) ✅
- [Phase 14 — Domain input validation](phase-14-domain-validation.md) ✅
- [Phase 15 — Config trust boundary](phase-15-config-trust.md) ✅
- [Phase 16 — Build and release hardening](phase-16-build-hardening.md) ✅
- [Phase 17 — UI robustness and cleanup](phase-17-ui-cleanup.md) ✅
- [Phase 18 — Accepted risks and review close-out](phase-18-accepted-risks.md) ✅
- [Phase 19 — Redact personal data from `OriginalSource/`](phase-19-redact-original-source-pii.md) ✅

## Cross-cutting notes

- [Master plan status note](master-plan-status-note.md) — what shipped after
  `docs/plan.md` was locked, and where the current truth lives.

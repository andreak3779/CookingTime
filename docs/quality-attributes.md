# Quality attributes

What this system optimises for, stated plainly rather than left to be worked out from the code and
the CI config. The decisions behind each one are in [decisions/](decisions/README.md). What the
project does not do is in [SCOPE-AND-LIMITS.md](SCOPE-AND-LIMITS.md); the structure is in
[architecture.md](architecture.md).

## Correctness

- **A whole class of bug is structurally impossible.** `Weight` and `CookingDuration` are
  `readonly record struct`, so the legacy bug where a setter wrote to the wrong field cannot be
  written. Pounds and kilograms meet at one conversion point, covered by a round-trip test.
- **Invalid input fails as data, not as an exception.** The calculator returns a `CookingResult`
  with validation errors for an empty meal, a weight of zero or less, an unknown meal or a weight
  outside a meal's range.
- **A bad catalog fails at startup, loudly.** Options are validated with `ValidateOnStart`, and the
  message names the entry that is wrong, instead of an unhandled component exception later.
- **Tests use the real types where it matters.** The regression tests for the weight-range bug drive
  the real `RangeMeal` end to end rather than a mock.

## Security

The application shape drives the posture: a static, client-side-only app with no server code, no
database, no accounts and no user data. A full-codebase review including an OWASP Top 10 (2021) pass
found no critical or high exploitable vulnerability; the findings were fixed or recorded as decisions
([ADR 0009](decisions/0009-accepted-security-risks.md)).

- **The served configuration is treated as public.** It is validated at startup, the `ResumeUrl` is
  limited to http and https (a `javascript:` URI used to be reachable from config and is now
  closed), and a guard test scans it for contact data and secret-shaped values.
- **A Content-Security-Policy** limits scripts, styles, fonts and images to the app's own origin.
  There is no inline script. The one exception, `'wasm-unsafe-eval'`, is what the .NET runtime
  needs to start.
- **No raw-HTML sink.** The code has no `MarkupString` or `Html.Raw`, so there is no place for
  injected markup to be rendered.
- **Absent by design, and written down.** No authentication, no cryptography and no SSRF surface are
  accepted decisions, each with a "revisit when" condition.

## Supply chain and reproducibility

- Exact package versions, a committed `packages.lock.json` for every project and `--locked-mode`
  restore in CI, so a dependency change is deliberate and reviewable.
- `NuGet.config` clears inherited sources and maps which packages may come from which feed.
  `global.json` pins the SDK.
- Every GitHub Action is pinned to a commit SHA. Dependabot proposes updates in grouped
  minor-and-patch pull requests, and major versions are left for a deliberate migration.
- CI audits the production project for vulnerable packages and checks that `dotnet publish` leaks no
  test assembly.

## Testing and CI gates

- Three tiers: unit (xUnit, Moq, FluentAssertions), component (bUnit) and integration
  (`WebApplicationFactory`). Run `dotnet test` for the current count; it was 256 on 2026-11-20.
- The build treats every warning as an error.
- CI fails if unit-test line coverage on production code drops below 75 %. Measured on 2026-11-20:
  unit 86.0 %, component 16.6 %, integration 70.0 %. Only the unit figure is gated.
- Guard tests pin properties that break quietly: package pinning, lock files, SHA-pinned Actions,
  config location, one component per route, no personal data in tracked files.

## Privacy

- The repository holds no personal contact data. The legacy source was redacted
  ([ADR 0010](decisions/0010-redact-pii-in-original-source.md)), and history was rewritten into a new
  repository with commits under the GitHub noreply address
  ([ADR 0011](decisions/0011-rewrite-history-into-a-new-repository.md)).
- A guard test scans every publishable file, binaries included, so a reintroduced leak fails CI.
- The app collects nothing: no analytics, no cookies and no network calls after load.

## Accessibility and usability

- A dark mode with a toggle that exposes its state through `aria-pressed` and an accessible label.
- Dark-palette contrast was measured and corrected to meet WCAG AA in both themes (Phase 12).
- Responsive layout, and self-hosted fonts so the app renders as designed with no external request.

## Operational model

- **Hosting:** static files. Any static host will do, as long as it falls back to `index.html` for
  deep links such as `/help`. There is no process to keep running and nothing to scale.
- **Observability:** the calculator logs an unknown-meal request (warning) and an out-of-range
  weight (debug), the two failures a user can cause that are not plain validation errors.
- **No SLA.** It is a portfolio project, not a service.

## Known deferred work

- Containerising and deploying to a local Kubernetes cluster is planned
  ([plan-kubernetes.md](plan-kubernetes.md), Phases 9 to 11). There is no `Dockerfile` or `k8s/`
  folder yet.
- The clickjacking directive (`frame-ancestors`) cannot be set from a meta CSP. It needs an HTTP
  header from whatever serves the files ([ADR 0009](decisions/0009-accepted-security-risks.md)).
- Major dependency upgrades (bUnit 2, coverlet 10, FluentAssertions 8) are deferred. The bUnit 1.x
  advisory on a transitive AngleSharp is suppressed in the test projects only, not in the shipped
  app.

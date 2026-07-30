# ADR 0006 — Coverage tooling (coverlet.collector + 75% threshold on UnitTests)

- **Status:** Accepted (Phase 7)

## Context

After Phase 5 we had 112 unit/component/integration facts but no machine-readable way to know whether a code change eroded coverage. The Phase 7 plan called for a blocking CI gate; the question is which coverage provider and what threshold.

## Decision

- **Coverage provider:** `coverlet.collector` 6.0.2 plus `coverlet.msbuild` 6.0.2. Hooks into `dotnet test` via the `XPlat Code Coverage` data collector and emits one Cobertura XML per test project.
- **Filter rules:** `coverlet.runsettings.xml` at the repo root. Includes `[CookingTime.Domain.*]`, `[CookingTime.Application.*]`, `[CookingTime.Infrastructure.*]`, `[CookingTime.Pages.*]`, `[CookingTime.Layout.*]`. Excludes the three test assemblies and `Backup.*`.
- **Threshold:** **75% line coverage on production code**, gated only on `CookingTime.UnitTests`. ComponentTests and IntegrationTests publish coverage as informational artifacts; they don't drive the gate.
- **Coverage script:** `scripts/check-coverage.py` parses Cobertura XML and exits non-zero on threshold miss. Pure Python (no external deps), runs in any `ubuntu-latest` runner.
- **CI integration:** `.github/workflows/ci.yml` runs the test suite per project, then the coverage script. Coverage XMLs are uploaded as the `coverage-cobertura` artifact.

## Consequences

**Easier:**
- A change that drops coverage below 75% fails CI before merge.
- Per-project XMLs let a future reader inspect coverage by test surface.
- The script is pure Python with no third-party packages — easy to extend (e.g., add a `--fail-on-decrease` flag).

**Harder:**
- Cross-project merge is not implemented. The 62.83% / 72.79% / 80.00% split (per project) is what we report, not a single merged number. Codecov-style dashboards are not in scope.
- Adding tests that only target Razor pages (ComponentTests, IntegrationTests) does not affect the gated UnitTests number. If a new contributor adds Razor pages without corresponding UnitTests, the gate still passes — by design.
- The script's line-count can double-count when both `<methods>/<method>/<lines>` and `<lines>` exist for the same class. This is cosmetic; the script's threshold is a percentage, so the absolute number is irrelevant.

**Neutral:**
- Branch coverage is collected but not gated. Line coverage is the contract.
- The `<NoWarn>` entries on test projects (NU1902, NU1903) already exist; coverlet 6.0.2 does not add new CVEs to the warnings-as-errors gate.

## References

- Phase 7 doc, "Validation" section — the baseline numbers (UnitTests 80.00%).
- Phase 7 doc, "Mid-flight fixes" — `coverlet.runsettings.xml` filter discovery, double-counting in line totals.
- [coverlet docs](https://github.com/coverlet-coverage/coverlet) — the canonical reference for `CollectCoverage` + `CoverletOutput` properties.
- [Cobertura format](https://github.com/cobertura-web/cobertura/blob/master/cobertura/src/site/markdown/cobertura_format.md) — the XML schema the script parses.
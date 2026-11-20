# Architecture Decision Records (ADRs)

Append-only. Never overwrite an old ADR — supersede with a new one that links back.

## Index

| # | Title | Phase | Status |
| --- | --- | --- | --- |
| 0001 | [Target framework: .NET 10](0001-target-net10.md) | Phase 0 | Accepted |
| 0002 | [Branch-per-phase with checkpoint commits](0002-branch-per-phase.md) | Phase 0 | Accepted |
| 0003 | [Docs as code](0003-docs-as-code.md) | Phase 0 | Accepted |
| 0004 | [SOLID + design-patterns expectations](0004-solid-and-patterns.md) | Phase 0 | Accepted |
| 0005 | [Test host entry-point naming (`TestHost.Main`, not `Program.Main`)](0005-test-host-entry-point-naming.md) | Phase 5 | Accepted |
| 0006 | [Coverage tooling (coverlet.collector + 75% threshold on UnitTests)](0006-coverage-tooling.md) | Phase 7 | Accepted |
| 0007 | [Design packages are reference artifacts; the Razor compile glob is explicit](0007-design-package-as-reference-artifact.md) | Phase 12 | Accepted |
| 0008 | [No personal contact data in shipped artifacts](0008-no-personal-contact-data.md) | Phase 13 | Accepted (decision 7 superseded by 0010; decision 5 by 0011) |
| 0009 | [Accepted security risks for a client-side-only application](0009-accepted-security-risks.md) | Phase 18 | Accepted (§6 superseded by 0010) |
| 0010 | [Redact personal data in `OriginalSource/` instead of keeping it verbatim](0010-redact-pii-in-original-source.md) | Phase 19 | Accepted (decision 5 superseded by 0011) |
| 0011 | [Purge personal data from history by publishing a rewritten copy as a new repository](0011-rewrite-history-into-a-new-repository.md) | Phase 20 | Accepted |

## Format

Each ADR is short (100–500 words) and answers three questions:

1. **Context.** What situation prompted the decision?
2. **Decision.** What did we choose?
3. **Consequences.** What trade-offs come with it? What becomes easier / harder?

References to larger docs (the master plan, a phase doc) are encouraged.

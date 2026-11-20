# ADR 0008 — No personal contact data in shipped artifacts

- **Status:** Accepted (Phase 13) — decision 7 superseded in part by
  [ADR 0010](0010-redact-pii-in-original-source.md); decision 5 superseded by
  [ADR 0011](0011-rewrite-history-into-a-new-repository.md)
- **Phase:** Phase 13
- **Deciders:** Project owner

## Context

A working-tree and git-history PII audit (method, evidence, and verified
corrections to an earlier draft: [../plan-remove-pii.md](../plan-remove-pii.md))
found personal contact data on four surfaces of a **public** repository:

1. **App config + rendered UI.** `appsettings.json` carried a personal email
   address under `ProductInfo:Email`, and `Pages/About.razor` rendered it as a
   `mailto:` link. Phase 13 also fixes a related defect: the Blazor WebAssembly
   SDK only bundles configuration that lives in `wwwroot/`
   (`<_BlazorConfigFile Include="wwwroot\appsettings*.json" />`), and the
   repository kept `appsettings.json` at its root, so the browser received **no
   configuration at all** — the meal catalog was empty in production. Moving
   the file into `wwwroot/` is the fix, and it is why this ADR matters:
   **once the file lives in `wwwroot/`, the server serves it to every visitor**,
   so a personal address in it becomes a live exposure rather than a dormant
   one. The removal below is what keeps it from becoming one.
2. **Git identity.** All 25 commits on `origin/main` carry the owner's personal
   email address as author; 7 local annotated tags carry it as tagger. Every
   published commit is stamped with contact data.
3. **Docs.** Tracked docs embed personal local filesystem paths
   (`/home/<user>/...`); one doc (`docs/LINKEDIN.md`) incorrectly describes the
   repository as private.
4. **Git history.** The address has been in published `appsettings.json` trees
   since `d574e18` (2026-07-29). Because it is reconstructible from the public
   repository handle, treating it as secretable was never viable from that
   date on.

`OriginalSource/frmAbout.cs` also carries a **third party's** email address,
inside the folder that repo rules require to stay verbatim (the Phase 8 doc;
AGENTS.md).

## Decision

1. **Remove `ProductInfo.Email` end-to-end** — the config line, the options
   property, the About-page `mailto:` block, the mirrored copy in the design
   package, and the test facts that assert it. Config-line-only removal is
   rejected: a dormant code path that can still publish the address is not
   removal.
2. **Pin the removal** with a regression-guard test asserting
   `ProductInfoOptions` has no `Email` property, so the option cannot silently
   return.
3. **Serve configuration from `wwwroot/` only.** `appsettings.json` moves into
   `wwwroot/` so the WebAssembly client actually receives it, and Phase 9's
   container document root is pinned to `wwwroot/` — never the publish root —
   so no root-level configuration file is ever served.
4. **Git identity uses GitHub's noreply form**
   (`<handle>+<numeric-id>@users.noreply.github.com`) for every future commit
   and tag. This is a repository-local setting the owner applies; tooling and
   CI apply it per-invocation (`git -c user.email=… commit`) wherever
   `.git/config` is not writable.
5. **No history rewrite by default.** The marginal benefit (the address is
   reconstructible from the public handle; scrapers have had months) does not
   justify rewriting published SHAs, invalidating the 8 merged PR references,
   and forcing re-clones. If the owner later wants it, the verified
   `git-filter-repo` procedure in the plan doc (§7-B) runs as its own approved
   task.
6. **Docs hygiene:** scrub personal local filesystem paths from tracked docs
   and correct the false privacy claim. The coverage XMLs CI publishes are
   **not** a PII surface: on a GitHub runner the source root is
   `/home/runner/work/<repo>/<repo>/`, which carries no owner data. (A local
   run does embed the local path, but those files are gitignored and never
   published.)
7. **`OriginalSource/` stays verbatim**, third-party contact strings included.
   Acting on them means deleting the folder wholesale under a new ADR — never
   in-place edits (Phase 8 rule).
   **Superseded in part by [ADR 0010](0010-redact-pii-in-original-source.md):**
   the folder now stays verbatim *except for personal data*, which is redacted
   in place, and the binary `.suo` IDE artifact — which carried a Windows
   user-profile path — is deleted.
8. **Redaction convention:** documents committed to this public repo refer to
   removed personal data descriptively ("the personal address"), never as the
   literal string.

## Consequences

**Easier**

- Deployed instances and rendered pages carry no personal contact data, and
  decision 3 keeps it that way once the app is containerized.
- The browser now receives real configuration, so the production meal catalog
  matches the configured one.
- Future contributors find one consistent policy instead of archaeology.
- The guard test converts the decision into a build-time invariant.

**Harder**

- Anyone expecting an `Email` link sees the option vanish; the `IOptions`
  binder ignores a stale key silently (no startup break, but a behavior change
  the phase doc must call out).
- Serving configuration from `wwwroot/` means the file is downloadable by any
  visitor. It must therefore never contain secrets — configuration that does
  is a separate concern, and the current file contains none.
- Published history still discloses what it discloses; only a future approved
  rewrite (plan doc §7-B) changes that.
- The noreply identity must be re-applied wherever `.git/config` cannot be
  written.
- The design package under `docs/design/` mirrors the About page, so it must
  be updated in the same change; otherwise a later theme re-apply reintroduces
  the removed block and breaks the build.

**Neutral**

- `dev@example.com`-style test fixtures stay (RFC 2606).
- The GitHub handle in CODEOWNERS/docs and the resume URL stay: the handle *is*
  the public identity of this project.

## References

- [../plan-remove-pii.md](../plan-remove-pii.md) — audit evidence, corrections
  to the earlier draft, full Phase 13 plan (§5), hygiene (§6), and the optional
  history-rewrite procedure (§7-B).
- [0002-branch-per-phase.md](0002-branch-per-phase.md) — one branch +
  checkpoint commit for Phase 13.
- [0003-docs-as-code.md](0003-docs-as-code.md) — plan doc + phase doc roles.
- [0007-design-package-as-reference-artifact.md](0007-design-package-as-reference-artifact.md)
  — why the design package must be updated in lockstep with the app.
- [../phases/phase-13-remove-email-setting.md](../phases/phase-13-remove-email-setting.md)
  — per-phase truth, created with the phase.

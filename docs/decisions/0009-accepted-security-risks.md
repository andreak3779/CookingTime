# ADR 0009 — Accepted security risks for a client-side-only application

- **Status:** Accepted (Phase 18)
- **Phase:** Phase 18
- **Deciders:** Project owner
- **Supersedes:** nothing. **Superseded by:** nothing.

## Context

Phases 14–17 were driven by a full-codebase review that included an OWASP
Top 10 (2021) pass. The review found no Critical or High *exploitable*
vulnerability — but it did find a set of properties that are **absent by
design**, and that a future reader could reasonably mistake for oversights.

This ADR records those absences as **decisions** rather than gaps, so that:

1. A reviewer does not re-report them as findings on every pass.
2. A future change that introduces a server, a database, or user content
   knows exactly which assumptions have just been invalidated.

The application's shape drives all of it: a **Blazor WebAssembly
single-page application** with **no server-side application code, no
database, no API endpoints, no authentication, and no user data**. Everything
it computes happens in the visitor's browser, from configuration that ships
with the app.

## Decision

The following are accepted, with the condition that triggers revisiting each.

### 1. No authentication or authorization (OWASP A07, A01)

The app has no accounts, no sessions, no cookies, no protected resources and
no privileged operations. `Microsoft.AspNetCore.Authorization` appears in the
build output only as a transitive framework assembly and is unused by
application code.

**Revisit when** the app gains a server, a database, or any user-specific
data. Until then there is nothing to protect and nothing to authenticate.

### 2. No cryptography (OWASP A02)

There is no `HttpClient`, no token, no secret, and no encrypted data
anywhere in the application. Transport security is entirely delegated to
the static host that serves the files.

**Revisit when** the app makes network requests or stores sensitive data.
Note that HTTPS/HSTS/`Referrer-Policy` belong to the **nginx layer** in the
Phase 9 container work, not to this repository.

### 3. No SSRF surface (OWASP A10)

SSRF requires a server-side component that fetches a URL derived from
attacker input. There is no server-side component at all: the WASM payload
executes in the visitor's browser. The two URL sinks —
`ProductInfo.ResumeUrl` and the `href="/help/*.htm"` links — are navigation
links the *browser* follows, never fetch targets. They cannot be coerced
into a server-side request.

**Revisit when** a server-side component is added that fetches URLs.

### 4. The client configuration file is public (OWASP A05, A04)

`wwwroot/appsettings.json` is served to every visitor. That is deliberate:
ADR 0008 moved it there because the Blazor WASM SDK only bundles
configuration that lives under `wwwroot/`, and without it the browser
received **no configuration at all** — the production meal catalog was
empty.

The consequence is that the file must never contain a secret. Phase 15
enforced that with a guard over secret *shapes* (`sk-`, `ghp_`, `AKIA`,
`-----BEGIN`, `password`, …), not just the `@` check that predated it, and
added startup validation so a malformed value fails immediately.

**Revisit when** a secret ever needs to exist in configuration — at which
point it must not be in this file.

### 5. No raw-HTML sink exists (OWASP A03)

The application contains **no** `MarkupString`, `Html.Raw`, or `@(Markup)`
— verified across every tracked file. Every dynamic value is rendered
through Razor's implicit encoding, so `Instructions`, `meal.Name`,
`FormattedDuration` and validation errors are all inert text.

This is the single most important XSS non-finding: it means there is no
place where an injected string could become markup. It is also a property
that a future change can destroy — any raw-HTML render must come with a
sanitisation decision.

**Revisit when** any component renders untrusted markup.

### 6. `OriginalSource/` retains a third party's contact address (OWASP A02)

> **Resolved in Phase 19.** This risk is **no longer accepted**. The contact
> strings in `OriginalSource/` are redacted and the binary `.suo` artifact that
> carried a Windows user-profile path is deleted — see
> [ADR 0010](0010-redact-pii-in-original-source.md), which supersedes this
> section and ADR 0008 decision 7 in part. The text below is kept as the
> historical record of the finding.

`OriginalSource/frmAbout.cs` contains a `mailto:` address that is neither
the owner's nor the application's. ADR 0008 decision 7 requires that folder
to stay verbatim; acting on it means deleting the folder wholesale under a
new ADR, never editing it in place.

The folder is **not compiled** (`EnableDefaultCompileItems=false`,
`DefaultItemExcludes`) and **not served** (it lives outside `wwwroot/`), so
it is not a runtime or publication surface.

**Revisit when** the owner chooses to delete the folder.

### 7. The clickjacking directive is not in the meta CSP (OWASP A05)

Phase 15 ships a Content-Security-Policy as a `<meta>` tag, because the app
is served as static files with no server-side header configuration. A
meta-delivered policy **ignores** the clickjacking directive — the browser
logs a warning and applies no protection — so including it would earn a
console warning and no security.

It belongs in an HTTP header, and the nginx layer in Phase 9 can send it.

**Revisit when** the container/nginx layer lands.

### 8. A `javascript:` URI was reachable from configuration — now closed

Recorded here because it is the shape of thing the others could take. Phase
15 found that `ProductInfo.ResumeUrl` reached an `href` with no scheme
allowlist, and that Razor's HTML-encoding does **not** neutralise a
*scheme*: `javascript:alert(1)` contains no encodable character and reaches
the browser verbatim.

An independent review verified in Chromium that `target="_blank"`
*incidentally* prevented execution on a real click — so the pre-fix code
was not exploitable by accident. But that is a side effect of an unrelated
attribute, not a control. The sink is now closed with an explicit http/https
allowlist at the validation layer, plus startup validation.

**Revisit when** any other configuration value reaches an `href`, `src`, or
event handler.

## Consequences

**Easier**

- A reviewer does not re-litigate absent authentication, cryptography, or
  SSRF defence on every pass — the answers are recorded, with the condition
  that would change them.
- The next person to add a server, a database, or user content has an
  explicit list of assumptions to revisit.
- The "no raw-HTML sink" property is stated as a property, so its
  destruction is a conscious decision rather than an accident.

**Harder**

- One more document to keep in sync with the ADRs.
- Each accepted risk carries a "revisit when" condition that someone has to
  notice. That is the cost of documenting absences rather than fixing them.

**Neutral**

- None of these decisions change any behaviour. They change what is
  *written down*.

## Verification

The claims above are not assertions — each was checked against the
repository:

| Claim | How it was verified |
| --- | --- |
| No auth, no crypto, no SSRF | Grep for `HttpClient`, `GetAsync`, `fetch`, `WebRequest`, `WebSocket`, credential and token patterns across production code — no matches outside `tests/` |
| No raw-HTML sink | Grep for `MarkupString`, `Html.Raw`, `@(Markup)`, `innerHTML`, `outerHTML`, `eval(`, `document.write` — zero matches across all tracked files |
| `OriginalSource/` not compiled or served | `EnableDefaultCompileItems=false` + `DefaultItemExcludes`; the folder is outside `wwwroot/` |
| The 4 legacy help pages are inert | All four read in full — no `<script>`, no `on*=` handler, no external resource |
| Config is public by design | ADR 0008; the SDK bundles only `wwwroot/appsettings*.json` |
| The meta CSP ignores the clickjacking directive | Observed in Chromium: "The Content Security Policy directive 'frame-ancestors' is ignored when delivered via a `<meta>` element" |

## References

- [../phases/phase-14-domain-validation.md](../phases/phase-14-domain-validation.md)
  — domain input validation.
- [../phases/phase-15-config-trust.md](../phases/phase-15-config-trust.md) —
  the config trust boundary: scheme allowlist, startup validation, CSP.
- [../phases/phase-16-build-hardening.md](../phases/phase-16-build-hardening.md)
  — supply-chain and release hardening.
- [../phases/phase-17-ui-cleanup.md](../phases/phase-17-ui-cleanup.md) — UI
  robustness and cleanup.
- [0008-no-personal-contact-data.md](0008-no-personal-contact-data.md) — the
  configuration-delivery decision this ADR depends on.
- [0007-design-package-as-reference-artifact.md](0007-design-package-as-reference-artifact.md)
  — why `OriginalSource/` and `docs/design/` are safe to keep in the repo.

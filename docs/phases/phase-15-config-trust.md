# Phase 15 — Config trust boundary

| | |
| --- | --- |
| **Branch** | `phase/15-config-trust` |
| **Cuts from** | `main` (which contains Phase 14) |
| **Checkpoint** | `checkpoint(phase-15): validate config at startup + allowlist ResumeUrl scheme + add CSP + harden served-config guard + phase doc` |
| **Tag** | `v0.15-config-trust` (after merge) |
| **Production code** | `Infrastructure/Configuration/ProductInfoOptions.cs` (scheme allowlist), `Infrastructure/Configuration/OptionsValidators.cs` (**new**), `Application/ServiceCollectionExtensions.cs` (`ValidateOnStart`), `Pages/About.razor` (guarded render), `wwwroot/index.html` (CSP), `wwwroot/appsettings.json` (strict JSON) |
| **Tests** | New `tests/CookingTime.UnitTests/Infrastructure/OptionsValidatorsTests.cs` (21 facts), new `tests/CookingTime.UnitTests/Infrastructure/ProductInfoOptionsTests.cs` (11 facts); updated `tests/CookingTime.UnitTests/Application/ServiceCollectionExtensionsTests.cs` (+3 facts), `tests/CookingTime.UnitTests/ClientConfigurationDeliveryTests.cs` (+13 facts), `tests/CookingTime.ComponentTests/AboutRazorTests.cs` (+5 facts), `tests/CookingTime.IntegrationTests/ThemeAssetsTests.cs` (+1 fact). **Total: 230 facts** (was 170) |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md`; updated `AGENTS.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test -c Release` → 230 facts green |

## Goal

`wwwroot/appsettings.json` is **served to every visitor** — ADR 0008 moved it
there deliberately, so the Blazor WASM SDK bundles it. That makes the file
part of the app's public surface, and it makes three questions worth
answering explicitly:

1. What happens if a value in it is hostile?
2. What happens if a value in it is malformed?
3. What is the app's defence if something hostile *does* reach the DOM?

This phase answers all three, and records the parts that are deliberately
absent.

## What shipped

### 1. `ResumeUrl` gets a scheme allowlist

The About page rendered a configuration value straight into an `href`:

```razor
<a href="@ProductInfo.ResumeUrl" target="_blank" rel="noopener">Resume</a>
```

Razor HTML-encodes attribute values, and that handles `"`, `<` and `>`. But
**HTML-encoding does not neutralise a scheme**. The payload
`javascript:alert(1)` contains no character that needs encoding, so it
reaches the browser verbatim and is treated as a script URI:

```
"javascript:alert(1)"    -> encoded: javascript:alert(1)
"a\"b"                    -> encoded: a&quot;b
"a<b"                     -> encoded: a&lt;b
```

There was no scheme allowlist anywhere: `ProductInfoOptions` had no
validation, and the section was bound with a plain `services.Configure<T>`.

`ProductInfoOptions` now owns the rule:

```csharp
private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];
```

with `TryGetSafeResumeUrl(out string? url)` and `HasSafeResumeUrl()`, and the
page renders the link only when the value passes:

```razor
@if (ProductInfo.TryGetSafeResumeUrl(out var resumeUrl))
{
    <p><a href="@resumeUrl" target="_blank" rel="noopener noreferrer">Resume</a></p>
}
```

**Honest scoping of the threat.** An independent review verified in Chromium
158 that `target="_blank"` happens to *prevent* a `javascript:` URI from
executing on a real click — so the pre-fix code was not exploitable by
accident. But that is a side effect of an unrelated attribute, not a designed
control, and it stops holding the moment someone edits the link. The sink is
now closed with an explicit rule instead of relying on luck.

The impact is bounded by design: `appsettings.json` ships *with* the app, so
poisoning it requires write access to the repository or the deployed artifact.
The point is that a bad value now **fails at startup** rather than rendering.

### 2. Configuration is validated at startup

Both options sections were bound with a plain `services.Configure<T>`, which
validates nothing. A malformed catalog entry therefore failed **late and in
the wrong place**: `MealFactory` throws from its constructor, the factory is
resolved lazily the first time the UI asks for the catalog, and the browser
shows an unhandled component exception with no indication of which config
line was wrong.

Two validators now live in `Infrastructure/Configuration/OptionsValidators.cs`
(its own file, so `MealCatalogOptions` and `ProductInfoOptions` stay plain
POCOs), registered with `ValidateOnStart()`:

| Rule | Why |
| --- | --- |
| Catalog entry must have a non-empty `Name` | A nameless meal cannot be selected |
| Catalog `Name`s must be unique (case-insensitive) | Duplicate names make the lookup ambiguous — the first match would win silently |
| Range meals need both `Min`/`MaxMinutesPerPound` | `MealFactory` already required this — it just failed late |
| Those minutes must be positive | Otherwise the cook time is zero or negative |
| `MaxMinutesPerPound >= MinMinutesPerPound` | An inverted range produces a duration whose minimum exceeds its maximum — the Phase 14 defect class, one layer up |
| `MinPounds > 0` and `MaxPounds >= MinPounds` | Same |

The failure message names the offending entry **by index**:

```
Meals:Catalog[1] ('Bad Meal') has MaxMinutesPerPound (40) less than
MinMinutesPerPound (45); the range would be inverted.
```

`ProductInfoOptions` gets the `ResumeUrl` rule from section 1.

### 3. A Content-Security-Policy

The app had none. It is served as static files with no server-side header
configuration, so the policy is delivered as a `<meta>` tag in
`wwwroot/index.html`:

```
default-src 'self';
script-src 'self' 'wasm-unsafe-eval';
style-src 'self' 'unsafe-inline';
img-src 'self' data:;
font-src 'self';
connect-src 'self';
object-src 'none';
base-uri 'self';
form-action 'self'
```

Two directives are **not optional** for this app, and getting either wrong
breaks the app outright:

- `script-src 'wasm-unsafe-eval'` — the .NET WASM runtime instantiates the
  Mono runtime through `WebAssembly.compile`. Without it the app refuses to
  boot.
- `style-src 'unsafe-inline'` — `Pages/Index.razor` uses inline
  `style="..."` attributes.

`'unsafe-inline'` is scoped to `style-src` **only**; `script-src` gets no
inline allowance, so the policy still blocks injected inline script.

**The policy was verified in a real browser, not just asserted.** A valid
WASM module was fetched and instantiated under the policy:

```
=== VALID wasm, NO CSP ===    WASM_INSTANTIATE_OK result=5
=== VALID wasm, WITH CSP ===  WASM_INSTANTIATE_OK result=5
```

Identical behaviour with and without the policy, and **no CSP violation
reported** — the `wasm-unsafe-eval` directive does its job.

The clickjacking directive is deliberately **omitted**: a meta-delivered
policy ignores it (Chromium logs "The Content Security Policy directive
'frame-ancestors' is ignored when delivered via a `<meta>` element"), so
including it would only produce a console warning. It belongs in an HTTP
header — see `docs/plan-kubernetes.md` for the nginx layer that can send it.

### 4. The served-config guard was too narrow

`ClientConfigurationDeliveryTests.Appsettings_ContainsNoPersonalContactData`
checked one thing: that the file contains no `@`. That catches an email
address and nothing else. An API key, a bearer token or a private-key header
has no `@` and would have passed it.

The guard is now a **13-case theory** over secret-shaped patterns —
`api_key`, `password`, `secret`, `bearer`, `-----BEGIN`, `sk-`, `ghp_`,
`AKIA`, `Authorization`, `client_secret` and variants — and the file must
also parse as strict JSON.

### 5. `wwwroot/appsettings.json` is now strict JSON

The file began with a `// filepath: appsettings.json` comment. The .NET JSON
provider tolerates it; **strict JSON does not**. Any tooling that parses the
file with a strict parser — a schema validator, a linter, a CI check, a
different runtime — would fail on it, and the failure would look like a bug
in the tool rather than a defect in the file.

The comment is removed and the file is plain JSON. The convention is
preserved where it belongs: the `// filepath:` marker lives at the top of
every `.cs` and `.razor` source file, and the config file's location is
already pinned by a test.

## Why no test caught these

| Layer | Result | Why |
| --- | --- | --- |
| `dotnet build` | green | None of these is a compile error. |
| `dotnet test` | 170 facts green | The suite asserted that `appsettings.json` was *served* and that `/fonts/x.woff2` was *served*, but never that the config was *validated* or that a hostile value was *rejected*. |
| Browser | the only place | A `javascript:` href and a late config failure both surface there. |

## New tests (60 facts)

| Suite | File | Facts | Covers |
| --- | --- | --- | --- |
| Unit | `Infrastructure/OptionsValidatorsTests.cs` | 21 | Catalog rules (blank/duplicate name, missing/non-positive/inverted minutes, pound bounds, offending index); `ProductInfo` accepts http/https, rejects 7 unsafe shapes |
| Unit | `Infrastructure/ProductInfoOptionsTests.cs` | 11 | The allowlist itself: http/https pass, 6 non-http schemes fail, blank fails, relative URLs fail, whitespace trimmed |
| Unit | `Application/ServiceCollectionExtensionsTests.cs` | +3 | A bad catalog and a `javascript:` URL both fail **at startup** with `OptionsValidationException`; a good catalog passes |
| Unit | `ClientConfigurationDeliveryTests.cs` | +13 | 11 secret-shaped patterns absent; file is strict JSON |
| Component | `AboutRazorTests.cs` | +5 | The link is **not rendered** for 4 unsafe schemes; it is rendered for https |
| Integration | `ThemeAssetsTests.cs` | +1 | The served shell declares the policy, with the two app-critical directives and no clickjacking directive |

The six sink tests (4 component + 2 startup-validation) were verified to
**fail** on the pre-fix files and **pass** after the fix.

## Validation

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)
dotnet test  -c Release   → UnitTests 178 · ComponentTests 21 · IntegrationTests 31
                             = 230 facts, 0 failed, 0 skipped   (was 170)
```

Browser verification (Chromium 158, against the running app):

```
CSP served in <head>                    ✓
WASM instantiation with the policy      ✓ (identical to no-policy behaviour)
CSP violations reported                 none
```

## Anti-patterns to avoid

- **Don't trust HTML-encoding to neutralise a URL.** Razor encodes `<`, `>`
  and `"` — it does not encode a *scheme*. `javascript:alert(1)` passes
  through untouched. Allowlist schemes at the point the value is validated,
  and render the link only when the value passes.
- **Don't rely on an unrelated attribute as a security control.**
  `target="_blank"` incidentally blocked script URIs; that is luck, not
  design. Do not build on it.
- **Don't bind configuration without validating it.** `Configure<T>` binds;
  it does not check. `ValidateOnStart()` turns a first-render exception into
  a startup failure that names the offending entry.
- **Don't put a directive in a `<meta>` CSP that the browser will ignore.**
  `frame-ancestors` is header-only — including it in a meta tag earns a
  console warning and no protection.
- **Don't guard a public file with a single pattern.** "No `@`" catches
  emails and nothing else. Deny the shapes that actually matter.
- **Don't put a marker in a data file that a strict parser will reject.**
  The `// filepath:` comment is a source-file convention; a `.json` file
  served to the browser is data.

## Out of scope

- Catalog lookup refactor and duplicate-name handling at the factory level
  — Phase 17 (the validator rejects duplicates at startup; the lookup is
  still `FirstOrDefault`).
- Pinning package versions, SHA-pinned GitHub Actions, publish hardening
  — Phase 16.
- The `Reset()` stale-validation defect — Phase 17.
- `ILogger` adoption — Phase 17.

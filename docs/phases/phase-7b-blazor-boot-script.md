# Phase 7b — Blazor WASM boot script reference

| | |
| --- | --- |
| **Branch** | `phase/7b-blazor-boot-script` |
| **Cuts from** | `main` (post-Phase 8) |
| **Checkpoint** | `checkpoint(phase-7b): fix Blazor WASM boot script reference + regression tests + phase doc` |
| **Tag** | `v0.7b-blazor-boot-script` (after merge into `main`) |
| **Production code** | `wwwroot/index.html` — one-line edit to the boot `<script>` tag |
| **Tests** | Updated `tests/CookingTime.IntegrationTests/RoutesSmokeTests.cs` assertion (was `blazor.web.js`, now `blazor.webassembly.js`); new `tests/CookingTime.IntegrationTests/BlazorBootScriptTests.cs` (3 facts). Total: **125 facts, 0 failed, 0 skipped**. |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Why this phase exists

After Phase 8 the build was green (`dotnet build -c Release`: 0 warnings, 0 errors), the 118 automated tests were green, and `dotnet run` started the dev server on `http://localhost:5000`. The page loaded in the browser, hung on the loading-progress SVG, and never bootstrapped. The Blazor error banner read **"An unhandled error has occurred."**

## Symptom

- `GET http://localhost:5050/` → 200, returns the SPA shell HTML.
- `GET http://localhost:5050/_framework/blazor.webassembly.js` → 200 (the runtime exists).
- `GET http://localhost:5050/_framework/CookingTime.m0dnmxx6xv.wasm` → 200 (the payload exists).
- `GET http://localhost:5050/_framework/blazor.web.js` → **404**.

The browser console showed `Failed to load resource: the server responded with a status of 404 (Not Found)` for `blazor.web.js`, then `Error: Could not find any elements appropriate for the Blazor boot script` (or the equivalent from the Blazor WASM loader).

## Root cause

[wwwroot/index.html:27](../../wwwroot/index.html) referenced `_framework/blazor.web.js`. The .NET 10 Blazor WebAssembly SDK only emits `_framework/blazor.webassembly.js` for a standalone Blazor WASM project. `_framework/blazor.web.js` is the boot script name used by **Blazor Server** and some .NET 6 templates — not by the current SDK in standalone mode.

The reason no test caught this:

- **UnitTests** (`tests/CookingTime.UnitTests`) exercise domain + application code; they don't touch the SPA shell.
- **ComponentTests** (`tests/CookingTime.ComponentTests`) render Razor components in-process via bUnit; they don't load `index.html` over HTTP.
- **IntegrationTests** (`tests/CookingTime.IntegrationTests`) use `WebApplicationFactory<Marker>` against the test host. The test host uses `UseBlazorFrameworkFiles()` + `MapFallbackToFile("index.html")` to serve the same `wwwroot/index.html`, so the *server* was faithfully returning the broken HTML and the existing smoke test ([RoutesSmokeTests.RazorRoute_Returns200_AndSpaShellAsync](../../tests/CookingTime.IntegrationTests/RoutesSmokeTests.cs)) was asserting the wrong reference name — the assertion matched the bug, not the truth.

In short: build, host, and tests all stayed green because they were each doing their job in isolation. Only a real browser, hitting the served URL, saw the 404.

## What shipped

### 1. `wwwroot/index.html` — boot script tag

```diff
-    <script src="_framework/blazor.web.js"></script>
+    <script src="_framework/blazor.webassembly.js"></script>
```

A comment above the line documents the rule for future maintainers and links back to this phase doc.

### 2. `tests/CookingTime.IntegrationTests/RoutesSmokeTests.cs` — corrected assertion

The `RazorRoute_Returns200_AndSpaShellAsync` test used to assert `html.Should().Contain("_framework/blazor.web.js")`. That assertion matched the bug. It now asserts the correct reference (`blazor.webassembly.js`) and explicitly asserts the absence of the legacy reference.

### 3. `tests/CookingTime.IntegrationTests/BlazorBootScriptTests.cs` — new (3 facts)

Dedicated regression class so the boot-script contract is locked in independently of the route-fallback smoke tests:

- `RootIndex_ReferencesBlazorWebAssemblyBootScriptAsync` — served HTML references `blazor.webassembly.js`.
- `RootIndex_DoesNotReferenceLegacyBlazorWebScriptAsync` — served HTML does **not** reference the legacy `blazor.web.js`.
- `BootScriptEndpoint_Returns200Async` — `GET /_framework/blazor.webassembly.js` returns 200 and a non-empty body.

### 4. Doc updates

- This file.
- `docs/phases/README.md` — Phase 7b row added.
- `README.md` — Phase 7b row added to the migration-journey table.

## Why no ADR

The Phase 7b change doesn't supersede or amend any previously recorded decision. ADR 0001 (target net10), 0002 (branch-per-phase), 0003 (docs-as-code), 0004 (SOLID + patterns), 0005 (test-host entry-point naming), 0006 (coverage tooling) all stand. Adding an ADR for a one-line `<script src>` fix would dilute the append-only ADR ledger.

## Validation

- `dotnet build -c Debug` — 0 warnings, 0 errors.
- `dotnet test --settings coverlet.runsettings.xml` — **125 facts, 0 failed, 0 skipped** (was 122 before Phase 7b; +3 from `BlazorBootScriptTests`).
- `python3 scripts/check-coverage.py … --threshold 75` — UnitTests line coverage on production code: **82.24%** (gate at 75%; +2.24 pp vs Phase 7 baseline of 80.00%).
- `dotnet run --urls=http://localhost:5050` + `curl http://localhost:5050/` — served HTML now references `_framework/blazor.webassembly.js`.
- Manual browser check — page boots, WASM payload downloads, the calculator form renders, the result section appears after submit.

## Anti-patterns to avoid

- **Don't pin the boot script path in multiple places.** The fix lives in `wwwroot/index.html` and the regression test reads the served HTML. Don't duplicate the path into a constant or a config file — that creates two sources of truth that can drift just like this bug did.
- **Don't add a `Properties/launchSettings.json` to "make the URL consistent."** That's a different problem; landing it here would couple two unrelated changes into one phase.
- **Don't merge this with a SDK pin (e.g. `Microsoft.AspNetCore.Components.WebAssembly` `10.0.0-*` → `10.0.0`).** One branch, one bug, one checkpoint, per [ADR 0002](../decisions/0002-branch-per-phase.md).

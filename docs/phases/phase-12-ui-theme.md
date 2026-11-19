# Phase 12 — CookingTime UI theme (design implementation)

| | |
| --- | --- |
| **Branch** | `phase/12-ui-theme` |
| **Cuts from** | `main` |
| **Checkpoint** | `checkpoint(phase-12): apply cookingtime-theme design + dark-mode toggle + phase doc` |
| **Tag** | `v0.12-ui-theme` (after merge) |
| **Production code** | `CookingTime.csproj` (`EnableDefaultRazorComponentItems=false`), `App.razor` (Router-only), `Layout/MainLayout.razor`, `Layout/ThemeToggle.razor` (new), `Pages/{Index,CookingTime,Help,About}.razor`, `wwwroot/index.html`, `wwwroot/css/cookingtime.css` (new), `wwwroot/js/theme.js` (new), `wwwroot/icons/` (new), `wwwroot/fonts/` (new) |
| **Tests** | New `tests/CookingTime.UnitTests/RazorRouteTableTests.cs` (4 facts), new `tests/CookingTime.ComponentTests/ThemeToggleRazorTests.cs` (6 facts), new `tests/CookingTime.IntegrationTests/ThemeAssetsTests.cs` (16 facts). **Total: 151 facts** (was 125) |
| **Docs** | This file; updated `docs/phases/README.md`; new [ADR 0007](../decisions/0007-design-package-as-reference-artifact.md); updated `docs/decisions/README.md`; updated `README.md`; updated `AGENTS.md` |
| **CI** | Unchanged. `dotnet build -c Release` → 0 warnings / 0 errors; `dotnet test` → 151 facts green; coverage gate at 75% → UnitTests **80.82%** |

## Goal

Apply the `cookingtime-theme` design package — a magenta + navy theme with four
restyled Razor pages, a stylesheet, and an icon set — to the live app. Ship the
dark palette with a working toggle, self-host the fonts so the app renders
correctly offline, and fix the two pre-existing defects the design's
`index.html` forced into the open (a missing favicon, and a `PageTitle` that
never reached the browser).

## What shipped

### 1. The design package became a reference artifact — and a latent bug was found

The design package arrived at `Layout/CookingTime UI design project/`. Placing
it there was **actively dangerous**.

`CookingTime.csproj` named its components explicitly:

```xml
<RazorComponent Include="Layout\**\*.razor" />
```

but that was **not** the only inclusion path. The Razor SDK also adds a default
glob of its own (`Sdk.Razor.CurrentVersion.props`):

```xml
<Content Include="**\*.razor" ... />
```

which feeds `EnableDefaultRazorComponentItems` — **default `true`**, and this
project had never disabled it. The Phase 6/8 csproj opts out of the default
`Compile` and `EmbeddedResource` items; `EnableDefaultRazorComponentItems` was
the one missing leg.

So **every** `.razor` file under the project root was compiled into the
production assembly, including the design package's four pages. All four of
their routes (`/`, `/cooking-time`, `/help`, `/about`) **exactly collided**
with the real pages.

#### Why every existing guard missed it

| Layer | Result | Why |
| --- | --- | --- |
| `dotnet build` | **0 warnings, 0 errors** | Two components claiming a route is not a compile error. |
| `dotnet test` | **125/125 green** | No test exercises the *client-side* route table. |
| `GET /` (server) | **HTTP 200** | The server serves the SPA shell for every route. |
| Browser | **throws** | `RouteTableFactory.DetectAmbiguousRoutes` throws `InvalidOperationException: The following routes are ambiguous`. |

The exception fires when the WASM router builds its route table — *after* the
payload boots, in the browser. Every guard in the repo was server-side or
compile-time, so all of them stayed green while the app was broken for its
actual users.

A second symptom confirmed the double-inclusion: with both globs active,
`App.razor` and every other live Razor file appeared **twice** in the
`RazorComponent` item list (the SDK de-duplicates at compile time, so this
stayed invisible too). The list held **97** entries where there should have
been **8**.

#### What did *not* work

The obvious mitigation is wrong:

```xml
<!-- DOES NOT WORK -->
<RazorComponent Remove="docs\**" />
```

A static `Remove` is evaluated at project-evaluation time. The SDK's default
items are added later, **inside the `ResolveRazorComponentInputs` target**, so
the `Remove` never sees them. Verified with a probe: a `.razor` file under
`docs/` still appeared as a `RazorComponent` with that `Remove` in place.

#### The fix

1. `git mv` the package to `docs/design/cookingtime-theme/`.
2. Set `<EnableDefaultRazorComponentItems>false</EnableDefaultRazorComponentItems>`.

This closes the second inclusion path and makes the component set **explicit** —
every `.razor` in the app is named in the csproj. It completes the opt-out the
project already performs for `.cs` and `.resx`.

**Verified:** `RazorComponent` items drop **97 → 8** with no duplicates; the
design types are **absent** from `bin/Release/net10.0/CookingTime.dll`
(`strings` shows no `design_project` / `cookingtime_theme`); `dotnet publish`
output contains no design leakage.

> **`docs/` is not intrinsically safe.** It is safe *only* because of the
> property. ADR 0007 records this.

### 2. Route-table regression test (4 facts)

`tests/CookingTime.UnitTests/RazorRouteTableTests.cs` reflects over the app
assembly and asserts **exactly one component per route template** — the same
invariant Blazor enforces at runtime. It also pins the expected four routes
and asserts no component comes from the design folders.

These facts were verified to actually catch the regression: with the design
package reintroduced under `Layout/`, **all four fail**; with it moved to
`docs/design/`, all four pass.

### 3. Theme stylesheet + self-hosted fonts

`wwwroot/css/app.css` → **`wwwroot/css/cookingtime.css`** (the design
stylesheet, with the Google Fonts `@import` replaced).

The theme's Google Fonts `@import` was removed and the two families are now
**self-hosted** under `wwwroot/fonts/` as four `woff2` files (~138 KB total):

| File | Family | Weights | Axes | Size |
| --- | --- | --- | --- | --- |
| `figtree-latin.woff2` | Figtree | 400–700 | `wght` 300–900 | 20 KB |
| `figtree-latin-ext.woff2` | Figtree | 400–700 | `wght` 300–900 | 10 KB |
| `bricolage-latin.woff2` | Bricolage Grotesque | 500–800 | `wght` 200–800, `opsz` 12–96 | 77 KB |
| `bricolage-latin-ext.woff2` | Bricolage Grotesque | 500–800 | `wght` 200–800, `opsz` 12–96 | 31 KB |

These are **variable** fonts (confirmed with `fontTools`: `fvar` is present),
so the `@font-face` rules use the **weight-range** syntax — `font-weight: 400 700`
and `font-weight: 500 800`. Declaring discrete weights would make the browser
pick one static instance and *synthesise* the rest.

`unicode-range` is copied verbatim from the upstream CSS so the `latin-ext`
file is only fetched when a character outside the Latin range is present.
`font-stretch: 100%` is preserved for Bricolage Grotesque (it has a width axis
upstream).

Both families are **SIL Open Font License 1.1** (`OFL.txt` confirmed in
`google/fonts`), which permits self-hosting. Provenance, upstream URLs and
regeneration steps are recorded in `wwwroot/fonts/OFFLINE-NOTICE.md`.

### 4. Dark-mode WCAG corrections (3 fixes)

The design's `[data-theme="dark"]` palette was shipped **as-is** but has three
computed WCAG AA failures. Shipping a toggle makes these user-reachable, so
they are fixed in this phase rather than deferred. Contrast maths over the CSS
tokens (AA needs 4.5:1 for body text):

| Selector | Was | Now | Fix |
| --- | --- | --- | --- |
| `.ct-nav__link.active` | **1.15:1** — active nav item invisible | 4.54:1 | Dark-mode override: `background: var(--ct-primary); color: var(--ct-primary-ink)` |
| `.ct-nav` (sidebar) | **1.00:1** — sidebar identical to `--ct-surface` | 1.16:1 | Dark-mode override: `background: var(--ct-surface-2)` |
| `.validation-message` | **3.33:1** — errors failed AA | **5.84:1** | `--ct-danger` redefined to `#FF6B81` in dark mode |
| `--ct-success` | 4.20:1 | **8.89:1** | Redefined to `#4FD8A4` alongside `--ct-danger` |

Light mode was checked too and is clean (body 16.39:1, labels 8.17:1, primary
button 4.90:1, active nav pill 15.27:1).

The overrides were verified against the **resolved cascade**, not just the
declaration text — the dark rules are declared *before* the base rules in the
file, and win on specificity rather than source order:

| Context | `--ct-danger` | `.ct-nav` bg | `.ct-nav__link.active` bg / color |
| --- | --- | --- | --- |
| Desktop, light | `#D6304A` | `var(--ct-navy)` | `var(--ct-bg)` / `var(--ct-navy)` |
| Desktop, dark | `#FF6B81` | `var(--ct-surface-2)` | `var(--ct-primary)` / `var(--ct-primary-ink)` |
| Mobile (≤767px), dark | — | `var(--ct-surface)` | `transparent` / `var(--ct-primary)` |

The mobile media query therefore keeps the bottom-bar layout intact rather
than inheriting the desktop sidebar treatment. Every resolved combination
clears WCAG AA in both themes (mobile dark: nav text 7.87:1, active link
4.54:1; mobile light: nav text 8.17:1, active link 4.90:1).

### 5. Icons + favicon fix

`wwwroot/icons/` now holds the design's 9 icons (5 PNG at 16/32/180/192/512,
4 SVG). All validated: PNG dimensions parsed from the file headers, SVGs
well-formed XML.

`wwwroot/favicon.ico` **did not exist** — both `index.html` and `App.razor`
referenced it, so every page load produced a 404. The references now point at
the SVG icon plus PNG fallbacks.

### 6. `index.html` + `App.razor` restructured

`App.razor` used to render a **second full HTML document** inside
`index.html`'s `#app` div, and referenced `_framework/blazor.web.js` — a file
the .NET 10 WASM SDK does not emit (only `blazor.webassembly.js` exists), so
that script 404'd.

Two consequences:

- the browser ignored the nested `<head>`, so `<HeadOutlet />` rendered into a
  document head nobody reads and **`<PageTitle>` never reached the browser tab
  title**;
- the nested `<body>` swallowed the real app markup.

`App.razor` is now **Router-only**, the shape the .NET 10 Blazor WASM template
uses. The `HeadOutlet` is registered as the `head::after` root component in
`Program.cs`, so `<PageTitle>` now lands in the real `<head>`.

`Routes.razor` was **removed** and its routing folded into `App.razor` — with
one Router there is nothing for a second component to do, and two Routers over
the same assembly would build two route tables. Nothing referenced it.

`wwwroot/index.html` now links the theme stylesheet, the icon set, and loads
`js/theme.js` as a **blocking** script in `<head>` so the persisted theme is
applied before the first paint.

### 7. Dark-mode toggle

- `wwwroot/js/theme.js` — the single implementation of "what is the current
  theme" and "how do we change it". Applies `data-theme` on `<html>` and
  persists to `localStorage` (`ct-theme`). `localStorage` access is wrapped:
  it throws in some privacy modes, and a missing preference must not break the
  app.
- `Layout/ThemeToggle.razor` — a thin Blazor wrapper over that script via
  `IJSRuntime`. It asks JavaScript for the live DOM state on first render and
  reflects the result in `aria-pressed` and the accessible label.

The state logic lives in the C# component rather than only in JS so it is
bUnit-testable; the JS file stays thin glue.

### 8. Restyled pages

`Pages/{Index,CookingTime,Help,About}.razor` and `Layout/MainLayout.razor` now
carry the design's markup and `ct-*` classes. The `ct-shell` grid gives a
240px navy sidebar on desktop and a bottom bar on mobile.

Every selector the existing tests assert on is preserved: `<h2>Cooking Time</h2>`,
`Cooking Instructions`, `validation-errors`, 3 `option`s, 2 radios
(Pounds/Kilograms), exactly 2 buttons (Calculate, Reset), and the
`InitialModel` / `SubmitAsync()` test hooks.

Two latent CSS defects in the design package were corrected during application:

- **`.ct-form .validation-errors` never matched.** The theme styles the error
  list with a *descendant* selector, but the design places the
  `<ul class="validation-errors">` **outside** the `</EditForm>` (verified by
  character-offset analysis: the `EditForm` spans chars 244–1752, the `ul` is
  at 2187). The list rendered as an unstyled `<ul>`. The theme's
  `.validation-errors:empty { display: none }` rule only makes sense if the
  list is *inside* the form, so the rule is now scoped to both `.ct-form` and
  `.ct-results`.
- **`ValidationSummary` rendered unstyled.** The framework's `ValidationSummary`
  renders `<div class="validation-errors">`, and the theme had no rule for it.

A cross-check of every `ct-*` class used in the design's markup against every
`ct-*` class defined in the stylesheet found **zero** used-but-undefined. The
13 unused classes (`ct-timeline`, `ct-chip`, `ct-toggle`, `ct-table`,
`ct-badge`, `ct-alert`, …) are design-system extras; they are kept — they cost
~1 KB and make the theme reusable.

### 9. Tests (26 new facts)

| Suite | File | Facts | Covers |
| --- | --- | --- | --- |
| Unit | `RazorRouteTableTests.cs` | 4 | One component per route template; expected routes; no design components |
| Component | `ThemeToggleRazorTests.cs` | 6 | Renders with theme classes; reads state from JS; `aria-pressed` reflects it; click calls `toggle`; accessible label |
| Integration | `ThemeAssetsTests.cs` | 16 | Served HTML links the stylesheet/icons/theme script; no external font CDN; no legacy `.ico` reference; script is blocking in `<head>`; all 9 assets serve 200 and are non-empty; CSS declares variable `@font-face` rules and the dark palette |

### 10. Docs

- New [ADR 0007](../decisions/0007-design-package-as-reference-artifact.md) —
  design packages are reference artifacts; the Razor compile glob is explicit.
- `docs/decisions/README.md` — ADR index updated.
- `docs/phases/README.md` — Phase 12 row + per-phase file list.
- `README.md` — status tables and test count updated.
- `AGENTS.md` — new anti-pattern row: reference `.razor` files under the project
  root get compiled into the app.

## Mid-flight fixes (and why they happened)

| Issue | Cause | Fix |
| --- | --- | --- |
| Design pages compiled into the app; all 4 routes ambiguous in the browser | Two independent Razor inclusion paths — the explicit `Layout\**\*.razor` glob **and** the SDK's default `Content Include="**\*.razor"` glob (`EnableDefaultRazorComponentItems` was `true`) | Move the package to `docs/design/` **and** set `EnableDefaultRazorComponentItems=false` |
| `<RazorComponent Remove="docs\**" />` had no effect | The SDK's default items are added *inside* the `ResolveRazorComponentInputs` target, after evaluation-time `Remove` runs | Do not rely on `Remove`; disable the default items instead |
| `App.razor` rendered a nested HTML document with a 404-ing `blazor.web.js` | Legacy Phase 1 boilerplate; Phase 7b fixed `index.html` but not `App.razor` | `App.razor` → Router-only (the .NET 10 template shape); `Routes.razor` removed |
| `<PageTitle>` never reached the browser tab | `HeadOutlet` rendered into the ignored nested `<head>` | Same as above — the real `<head>` now receives it |
| `favicon.ico` 404'd on every page load | The file was referenced but never existed | Reference the SVG icon + PNG fallbacks instead |
| `.ct-form .validation-errors` never matched | The design places the error list outside the `</EditForm>` | Scoped the rule to `.ct-form` **and** `.ct-results` |
| Dark palette failed WCAG AA in 3 places | The dark token block did not redefine `--ct-danger` / `--ct-success`, and reused `--ct-navy` for both the sidebar and the active nav item | 3 dark-mode CSS overrides; all now ≥ 4.5:1 |
| `wwwroot/index.html` comments broke two integration tests | The comments literally contained `favicon.ico` and `defer`, which the tests assert are **absent** | Reworded the comments (the tests were right — a reference in a comment still trips a substring assertion) |
| bUnit rejected a hand-rolled `IJSRuntime` fake | bUnit intercepts `IJSRuntime` itself and fails unconfigured calls | Used bUnit's `JSInterop.Setup<T>(identifier).SetResult(value)` API |
| `Setup<bool>(id, _ => value)` did not compile | The matcher overload's result type is `bool`; the value overload is `Setup<T>(id, params object[])` | `Setup<bool>(id).SetResult(value)` |

## Validation

```
dotnet build -c Release   → 0 Warning(s), 0 Error(s)   (all 4 projects)
dotnet test  -c Release   → UnitTests 108 · ComponentTests 15 · IntegrationTests 28
                             = 151 facts, 0 failed, 0 skipped   (was 125)
coverage gate             → UnitTests 514/636 lines = 80.82%  (gate 75%)  PASS
```

The design-regression test was verified to **fail** when the defect is
reintroduced and **pass** when it is fixed — it is a real guard, not a
formality.

## Anti-patterns to avoid

- **Don't put `.razor` files under the project root that are not app
  components.** Reference material, design packages, and scratch copies get
  compiled into the app. `docs/` is safe only because
  `EnableDefaultRazorComponentItems=false` — see ADR 0007.
- **Don't rely on `<RazorComponent Remove="..." />`** to keep a folder out of
  the build. It does not work against the SDK's default items.
- **Don't add a second `<Router>`** over the same assembly. It builds a second
  route table; `App.razor` owns routing.
- **Don't re-add a `<!DOCTYPE html>` to `App.razor`.** The host document lives
  in `wwwroot/index.html`; a second document breaks `<PageTitle>` and nests
  markup illegally.
- **Don't load `js/theme.js` at the end of `<body>` or with `defer`.** The
  persisted theme must be applied before first paint or dark-mode users get a
  flash of the light theme on every load.

## Out of scope

- PWA `manifest.webmanifest` + service worker (the icons ship; the manifest
  wiring is not added here).
- A settings page for the theme (the toggle lives in the nav).
- Domain / application layer changes — this phase is presentation only.
- Reconciling `main` and `upgrading-to-net-10` (3 commits on one branch, 12 on
  the other). This phase cuts from `main` and targets `main`.

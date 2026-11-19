# CookingTime theme

Built against the local CookingTime source (Pages/, Layout/, wwwroot/).

## Install
1. Copy `cookingtime.css` to `wwwroot/css/` (it replaces `app.css`; delete `app.css`) and `icons/` to `wwwroot/icons/`.
2. Copy `razor/wwwroot/index.html` over `wwwroot/index.html` (adds icon links, loads `cookingtime.css`, keeps `blazor.webassembly.js`).
3. Copy `razor/Layout/MainLayout.razor` and `razor/Pages/*.razor` over the existing files.
4. Optional favicon: put `icons/icon-32.png` as `wwwroot/favicon.ico` equivalent; SVG icon is linked already.

## Test compatibility
The restyle keeps what the existing tests assert: `<h2>Cooking Time</h2>`, `Cooking Instructions`, `validation-errors`, 3 `option`s for 2 meals, 2 radios (Pounds, Kilograms), exactly 2 buttons (Calculate, Reset), the `@code` block, and the SPA shell in `index.html`. Only markup/classes changed. Run `dotnet test` to confirm.

## Dark mode
Set `data-theme="dark"` on `<html>` (JS interop from a future Settings toggle).

PWA manifest icons: `icon-192.png`, `icon-512.png`.

## Divergence from the live app (read before re-applying)

This package is a **reference snapshot**, not a live source. The app's copy of
the theme has since diverged in two ways that matter when copying files back:

1. **Fonts are self-hosted in the app.** The `cookingtime.css` here still
   carries the Google Fonts `@import`. The app replaces it with `@font-face`
   rules pointing at `wwwroot/fonts/*.woff2` so the app renders offline. If
   you re-copy this CSS, restore the `@font-face` block from
   `wwwroot/css/cookingtime.css` — and keep the paths **relative to the
   stylesheet**, i.e. `url('../fonts/…')`. A CSS `url()` resolves against the
   stylesheet's own URL (`/css/`), so `url('fonts/…')` resolves to
   `/css/fonts/…` and 404s.

2. **The app has a theme toggle.** `wwwroot/js/theme.js` and
   `Layout/ThemeToggle.razor` are not part of this package.

See `docs/phases/phase-12-ui-theme.md` → *Post-merge defect fixes* for the
full story.

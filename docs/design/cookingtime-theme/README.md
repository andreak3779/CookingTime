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

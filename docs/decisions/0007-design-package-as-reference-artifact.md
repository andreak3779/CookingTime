# ADR 0007 — Design packages are reference artifacts, and the Razor compile glob is explicit

- **Status:** Accepted (Phase 12)

## Context

Phase 12 applied the `cookingtime-theme` design package (a magenta + navy theme: 4 razor pages, `cookingtime.css`, 9 icons) to the live app. The package was originally delivered as a folder of reference material placed at `Layout/CookingTime UI design project/`.

That placement turned out to be **actively dangerous**, and the reason is worth recording carefully because the failure is silent.

### The failure

`CookingTime.csproj` named its razor components explicitly:

```xml
<RazorComponent Include="Layout\**\*.razor" />
<RazorComponent Include="Pages\**\*.razor" />
```

But that was **not** the only inclusion path. The Razor SDK adds a default glob of its own (`Sdk.Razor.CurrentVersion.props`):

```xml
<Content Include="**\*.razor" ... />
```

which feeds `EnableDefaultRazorComponentItems` (default `true`). This project had **not** disabled it — the Phase 6/8 csproj disables the default `Compile` and `EmbeddedResource` items, but `EnableDefaultRazorComponentItems` was the one missing leg.

Consequently **every** `.razor` file under the project root was compiled into the production assembly, including the design package's four pages. All four of their routes (`/`, `/cooking-time`, `/help`, `/about`) **exactly collided** with the real pages.

### Why it was invisible

| Layer | Result | Why |
| --- | --- | --- |
| `dotnet build` | **0 warnings, 0 errors** | Two components claiming a route is not a compile error. |
| `dotnet test` | **125/125 green** | No test exercises the *client-side* route table. |
| `GET /` (server) | **HTTP 200** | The server serves the SPA shell for every route. |
| Browser | **throws** | `RouteTableFactory.DetectAmbiguousRoutes` throws `InvalidOperationException: The following routes are ambiguous`. |

The exception fires when the WASM router builds its route table — *after* the payload boots, in the browser. Every existing guard was server-side or compile-time, so all of them stayed green while the app was broken for its actual users.

Two further symptoms confirmed the double-inclusion: with both globs active, `App.razor` and every other live razor file appeared **twice** in the `RazorComponent` item list (the SDK de-duplicates at compile time, so this stayed invisible too).

### What did *not* work

The obvious mitigation is wrong:

```xml
<!-- DOES NOT WORK -->
<RazorComponent Remove="docs\**" />
```

A static `Remove` is evaluated at project-evaluation time. The SDK's default items are added later, **inside the `ResolveRazorComponentInputs` target**, so the `Remove` never sees them. Verified with a probe: a `.razor` file under `docs/` still appeared as a `RazorComponent` with that `Remove` in place.

## Decision

**1. Design packages live under `docs/design/`, and are reference artifacts — never app inputs.**

```
docs/design/cookingtime-theme/
├── README.md
├── cookingtime.css
├── icons/            (9 files: 5 PNG + 4 SVG)
└── razor/            (MainLayout.razor + 4 pages + wwwroot/index.html)
```

The theme is applied by **copy**: the files under `docs/design/` are a *snapshot* of the design, not a live source. A change to the design package does not change the app until someone deliberately re-applies it. This is intentional — a design snapshot that silently feeds the build is exactly the failure above.

**2. `CookingTime.csproj` sets `<EnableDefaultRazorComponentItems>false</EnableDefaultRazorComponentItems>`.**

This closes the second inclusion path and makes the component set **explicit** — every `.razor` in the app is named in the csproj. It completes the opt-out the project already performs for `.cs` and `.resx`.

> **`docs/` is not intrinsically safe.** It is safe *only* because of this property. Placing a design package anywhere under the project root without the property set re-opens the hole.

**3. A regression test asserts one component per route template.**

`tests/CookingTime.UnitTests/RazorRouteTableTests.cs` reflects over the app assembly and fails if any route template is claimed by more than one component. This is the automated proxy for the runtime invariant Blazor enforces, and it covers a layer the server-side test suite structurally cannot reach.

## Consequences

**Easier:**
- The component set is explicit and greppable — 8 items, no hidden glob.
- The ambiguous-route class of bug is now caught by a unit test, not by a user in a browser.
- Design packages can be versioned and reviewed as reference material without any risk of entering the build.
- The csproj now has a **uniform** policy: no default `Compile`, no default `EmbeddedResource`, no default `RazorComponent` items.

**Harder:**
- Adding a new page means adding an explicit `<RazorComponent Include="..." />` line to the csproj. This is a deliberate trade: a forgotten line fails *loudly* (the page simply does not exist / has no route) rather than silently duplicating a route.
- `docs/design/` will drift from the applied theme. ADR 0003's "supersede, don't overwrite" rule applies to it like any other doc.

**Neutral:**
- The `docs/design/` copy costs ~40 KB in the repo and nothing at runtime.

## References

- Phase 12 doc, "Mid-flight fixes" — the probe methodology that isolated the two inclusion paths.
- Blazor `release/10.0` source, `src/Components/Components/src/Routing/RouteTableFactory.cs` — `DetectAmbiguousRoutes`, which throws the `InvalidOperationException`.
- `Sdk.Razor.CurrentVersion.props` — the default `Content Include="**\*.razor"` glob.
- [ADR 0003](0003-docs-as-code.md) — docs-as-code; per-phase truth lives in `docs/phases/`.
- [ADR 0006](0006-coverage-tooling.md) — the coverage filter rules that keep `docs/` out of the coverage report.

# Phase 1 — Blazor host

| | |
| --- | --- |
| **Branch** | `phase/1-blazor-host` |
| **Cuts from** | `upgrading-to-net-10` (Phase 0 merged) |
| **Checkpoint** | `checkpoint(phase-1): scaffold blazor host + host build test + phase doc` |
| **Tag** | `v0.1-blazor-host` (after merge into `upgrading-to-net-10`) |
| **Production code** | `Program.cs`, `App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/MainLayout.razor`, `Pages/Index.razor`, `Pages/CookingTime.razor`, `Pages/About.razor`, `wwwroot/index.html`, `wwwroot/css/app.css`, replaced `CookingTime.csproj`, replaced `CookingTime.sln` |
| **Tests** | `tests/CookingTime.UnitTests/HostBuildTests.cs` (3 facts) — assembly loads, targets net10.0, entry point reachable |
| **Docs** | This file; updated `docs/phases/README.md`; updated `README.md` |

## Goal

Stand up a working ASP.NET Core Blazor WebAssembly host on .NET 10 so that subsequent phases can layer domain, application, and UI without re-litigating the build infrastructure.

## What shipped

### 1. `CookingTime.csproj` (replaced)

New SDK-style Blazor WebAssembly csproj targeting `net10.0` with the conventions spelled out in `AGENTS.md`:
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<InvariantGlobalization>true</InvariantGlobalization>` (smaller WASM payload; no ICU dependency at runtime)

To coexist with the legacy WinForms-era files left on disk (`App.ico`, `frmAbout.cs`/`frmAbout.resx`, `frmCookingTime.cs`/`frmCookingTime.resx`, `ComCookingTime.cs`/`ComCookingTime.resx`, `AssemblyInfo.cs`, `Models/`, `Properties/`, `Data/`, etc.), the csproj **disables the SDK's default item globs** and lists only the new Blazor app sources explicitly:

```xml
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
<EnableDefaultRazorGenerateItems>false</EnableDefaultRazorGenerateItems>
...
<RazorComponent Include="App.razor" />
<RazorComponent Include="Routes.razor" />
<RazorComponent Include="_Imports.razor" />
<RazorComponent Include="Layout\**\*.razor" />
<RazorComponent Include="Pages\**\*.razor" />
```

This keeps the legacy files tracked (historical reference) but stops the SDK from auto-globbing them into the Blazor build — which avoids the `MSB3577` "two output file names resolved to the same path" and `MSB3822` "non-string resources require `System.Resources.Extensions`" errors.

Also: `<InternalsVisibleTo Include="CookingTime.UnitTests" />` so the test project can reference the synthesized `Program` type.

### 2. Blazor host files

| File | Purpose |
| --- | --- |
| `Program.cs` | `WebAssemblyHostBuilder.CreateDefault(args)`; registers `<App>` and `<HeadOutlet>`; `await builder.Build().RunAsync()`. |
| `App.razor` | Static HTML host with `<base href="/" />`, `<HeadOutlet />`, `<Routes />`, and the Blazor runtime script. |
| `Routes.razor` | `<Router>` bound to `typeof(Program).Assembly`; uses `Layout.MainLayout` as default. |
| `_Imports.razor` | Implicit Razor usings (no `@using CookingTime` — would clash with the generated `App` type in the same namespace). |
| `Layout/MainLayout.razor` | Three-link nav (Home / Cooking Time / About) + `@Body`. |
| `Pages/Index.razor` | `@page "/"` — home placeholder linking to `/cooking-time`. |
| `Pages/CookingTime.razor` | `@page "/cooking-time"` — placeholder, says "Coming in Phase 4". |
| `Pages/About.razor` | `@page "/about"` — minimal content; will bind to options in Phase 4. |

### 3. `wwwroot/`

| File | Purpose |
| --- | --- |
| `wwwroot/index.html` | Single Blazor host page — `<div id="app">` with the loading indicator; references `css/app.css` and `_framework/blazor.web.js`. |
| `wwwroot/css/app.css` | Minimal stylesheet (~70 lines): system fonts, blue links, light-gray background, the standard Blazor loading spinner, and the `blazor-error-ui` overlay. |
| `wwwroot/help/*.htm` | The four legacy HTML help files moved from `help/` so they're servable by the Blazor host. Phase 4 will wire the `Pages/Help.razor` route. |

### 4. Test project (`tests/CookingTime.UnitTests/`)

New xUnit test project targeting `net10.0` with three facts in `HostBuildTests.cs`:

- `MainAssembly_LoadsSuccessfully` — `typeof(Program).Assembly` is non-null and named `CookingTime`.
- `MainAssembly_TargetsNet10` — `[TargetFrameworkAttribute].FrameworkName` starts with `.NETCoreApp,Version=v10.0`.
- `Program_EntryPointType_IsAccessible` — `typeof(Program)` is non-null, name is `Program`, and assembly is `CookingTime`.

The original plan called for a `WebApplicationFactory<Program>`-based smoke test against `/`. That **does not work for Blazor WebAssembly** because `WebApplicationFactory` expects an ASP.NET Server host. WASM has no server-side host at runtime; the test factory complains about a missing `CookingTime.deps.json`. We **dropped that approach in favor of the assembly-level assertions above**, which proves the host builds, targets the right framework, and exposes the entry point. Real HTTP integration tests for the running app land in Phase 5 via bUnit feature flows.

> Note: `Microsoft.AspNetCore.Mvc.Testing` was originally added to the test csproj and then removed — it's unused in Phase 1 and will be re-added in Phase 5 if we end up needing it.

### 5. Solution file

Replaced the legacy WinForms `CookingTime.sln` with the .NET 10 XML solution format `CookingTime.slnx`:

```xml
<Solution>
  <Folder Name="/tests/">
    <Project Path="tests/CookingTime.UnitTests/CookingTime.UnitTests.csproj" />
  </Folder>
  <Project Path="CookingTime.csproj" />
</Solution>
```

Note: `dotnet new sln` in SDK 10.0.110 defaults to the new `.slnx` (XML) format. Old `CookingTime.sln` removed.

### 6. Legacy files — still on disk, not in the Blazor build

The following legacy WinForms-era files remain tracked for historical reference and will be removed in **Phase 6 (cleanup)**:

- `App.ico`, `AssemblyInfo.cs`, `ComCookingTime.cs`/`ComCookingTime.resx`, `CookingTime.resx`, `frmAbout.cs`/`frmAbout.resx`, `frmCookingTime.cs`/`frmCookingTime.resx`, `meal.xml`, `UpgradeLog*.XML`, `_UpgradeReport_Files/`, `Models/`, `Properties/`, `Data/`
- `OriginalSource/CookingTime.csproj`, `OriginalSource/CookingTime.sln`, `OriginalSource/*.cs`/`*.resx` — these are the canonical historical reference, never built.

## Verification

```
dotnet restore          → 2 projects restored in ~13s
dotnet build -c Release → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet test -c Release  → Passed! Failed: 0, Passed: 3, Total: 3
dotnet run --urls=http://127.0.0.1:5180 → boots, HTTP 200 on /, /cooking-time, /about
```

The `dotnet run` smoke confirms the static `wwwroot/index.html` is served, the Blazor app would launch in a browser, and the routing table resolves all three registered pages.

## Out of scope

- Real `EditForm` + DataAnnotations on `CookingTime.razor` → Phase 4.
- `About.razor` bound to options → Phase 4.
- Domain layer → Phase 2.
- Application service → Phase 3.
- bUnit component tests → Phase 4.
- Feature / integration tests → Phase 5.
- Removal of legacy files → Phase 6.
- CI workflow → Phase 7.

## Decisions worth recording for future agents

- **`.slnx` not `.sln`.** The .NET 10 SDK defaults to the new XML solution format; we kept that default rather than forcing the old binary format.
- **No `Microsoft.AspNetCore.Mvc.Testing` in Phase 1.** It doesn't fit Blazor WASM; we'll add it back in Phase 5 only if needed.
- **No `WebApplicationFactory<Program>` test.** Same reason. Replaced with assembly-level smoke tests.
- **Legacy WinForms files left on disk intentionally.** Phase 6 deletes them; until then they remain as historical reference, excluded from the Blazor build via disabled default item globs in the csproj.

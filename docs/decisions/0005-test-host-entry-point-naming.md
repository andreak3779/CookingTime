# ADR 0005 — Test host entry-point naming (`TestHost.Main`, not `Program.Main`)

- **Status:** Accepted (Phase 5)

## Context

`Microsoft.NET.Test.Sdk` auto-generates a `Program` class with a `Main(string[])` entry point in every test project. When a test project is *also* a Web host (so that `WebApplicationFactory<TEntryPoint>` can spin up an in-process `TestServer`), the host's `Main` collides with the test SDK's generated `Program.Main`, producing `CS0017: Program has more than one entry point`. We hit this in Phase 5 when the first attempt put the host's `Main` in a `Program` class.

Even when the build *does* succeed (e.g. by renaming the host class to `Program` and getting lucky with namespace resolution), the next `Microsoft.NET.Test.Sdk` upgrade can re-surface the collision. The fix has to be durable.

## Decision

Test-only Web hosts in this repo must follow three rules:

1. **The host class is named `TestHost`, not `Program`.** Lives in the project's root namespace (e.g. `CookingTime.IntegrationTests.TestHost`).
2. **`WebApplicationFactory` is bound to a separate `Marker` class** (e.g. `CookingTime.IntegrationTests.Marker`), not to `TestHost`. The `Marker` class is the type the factory's generic parameter refers to; the factory inspects the *assembly* (not the marker type) to find the host entry point, but the marker keeps the API intent explicit.
3. **The csproj sets `<StartupObject>CookingTime.IntegrationTests.TestHost</StartupObject>`** so the host's `Main` is the assembly's entry point, not the test SDK's auto-generated `Program.Main`.

The canonical pattern is:

```csharp
// TestHost.cs
public static class TestHost
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddCookingTime(builder.Configuration);
        var app = builder.Build();
        app.UseStaticFiles();
        app.UseBlazorFrameworkFiles();
        app.MapFallbackToFile("index.html");
        app.Run();
    }
}

// Marker.cs
public sealed class Marker;
```

```xml
<!-- CookingTime.IntegrationTests.csproj -->
<PropertyGroup>
  <StartupObject>CookingTime.IntegrationTests.TestHost</StartupObject>
</PropertyGroup>
```

## Consequences

**Easier:**
- The host entry point is durable across `Microsoft.NET.Test.Sdk` upgrades — the test SDK's `Program.Main` coexists with `TestHost.Main` because they are in different classes.
- A reviewer sees `TestHost` in a stack trace and immediately knows where to look.
- The `Marker` type makes the `WebApplicationFactory<Marker>` call site self-documenting.

**Harder:**
- `dotnet run` on the test project launches the test runner, not the host (because `IsTestProject=true` instructs the SDK to emit the test discovery entry point). To smoke the host manually, run the test project under `Microsoft.AspNetCore.Components.WebAssembly.DevServer` or temporarily clear `IsTestProject`. For CI, the `WebApplicationFactory` path is the source of truth — we do not require a manual smoke step.
- Future readers who copy the pattern from a tutorial that uses `Program.Main` will hit the collision again. The Phase 5 doc's "Anti-patterns to avoid" section calls this out.

**Neutral:**
- The cost of the pattern is one small `Marker` class per test-host project. Trivial.

## References

- Phase 5 doc, "Mid-flight fixes recorded" — the original `CS0017` collision.
- Phase 5 doc, "Anti-patterns to avoid" — explicit guidance to future contributors.
- ASP.NET Core docs, [`WebApplicationFactory<TEntryPoint>`](https://learn.microsoft.com/aspnet/core/test/integration-tests) — the canonical pattern this ADR adapts.

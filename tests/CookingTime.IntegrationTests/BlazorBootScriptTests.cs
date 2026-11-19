// filepath: tests/CookingTime.IntegrationTests/BlazorBootScriptTests.cs
namespace CookingTime.IntegrationTests;

/// <summary>
/// Regression tests for the Blazor WebAssembly boot script reference.
/// Phase 7b fixed a bug where <c>wwwroot/index.html</c> referenced
/// <c>_framework/blazor.web.js</c>, but the .NET 10 Blazor WebAssembly
/// SDK only emits <c>_framework/blazor.webassembly.js</c>. The browser
/// received a 404 on the boot script and the WASM payload never
/// bootstrapped; the unit/component/integration tests stayed green
/// because none of them load <c>index.html</c>. These tests pin both
/// the served HTML and the runtime URL so the regression cannot
/// silently come back.
/// </summary>
public sealed class BlazorBootScriptTests : IClassFixture<CookingTimeAppFactory>
{
    private readonly CookingTimeAppFactory _factory;

    public BlazorBootScriptTests(CookingTimeAppFactory factory) => _factory = factory;

    [Fact]
    public async Task RootIndex_ReferencesBlazorWebAssemblyBootScriptAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("src=\"_framework/blazor.webassembly.js\"",
            because: "the SPA shell must reference the boot script emitted by the .NET 10 Blazor WebAssembly SDK");
    }

    [Fact]
    public async Task RootIndex_DoesNotReferenceLegacyBlazorWebScriptAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        // Defensive: even a future regression that points at the new script
        // would still be broken if it ALSO pointed at the old one, so we
        // lock the absence explicitly.
        html.Should().NotContain("src=\"_framework/blazor.web.js\"",
            because: "blazor.web.js is the Blazor Server / .NET 6 boot script and is not emitted by the standalone Blazor WebAssembly SDK");
    }

    [Fact]
    public async Task BootScriptEndpoint_Returns200Async()
    {
        using var client = _factory.CreateClient();

        // The runtime injects a hashed filename in the actual served HTML
        // (e.g. blazor.webassembly.<hash>.js). For the boot script emitted
        // by the SDK the canonical un-hashed name is what's served.
        var response = await client.GetAsync("/_framework/blazor.webassembly.js");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "the browser must be able to download the boot script so the WASM payload can bootstrap");
        (await response.Content.ReadAsStringAsync()).Should().NotBeEmpty(
            because: "the boot script is non-empty in every released Blazor WebAssembly SDK");
    }
}

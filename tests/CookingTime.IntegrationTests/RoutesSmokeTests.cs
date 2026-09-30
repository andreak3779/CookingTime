// filepath: tests/CookingTime.IntegrationTests/RoutesSmokeTests.cs
namespace CookingTime.IntegrationTests;

/// <summary>
/// HTTP smoke tests against the Phase 5 test-only host. The host serves
/// the Blazor WASM client via <c>UseBlazorFrameworkFiles</c> +
/// <c>MapFallbackToFile</c>, which means every route returns the same
/// SPA shell HTML — the page content is rendered client-side after the
/// WASM bundle boots. These tests therefore assert the *server's*
/// responsibilities: fallback routing, static file serving, and the
/// absence of 404s. End-to-end rendering of the pages is covered by
/// <see cref="CookingTimeFeatureTests"/> via bUnit.
/// </summary>
public sealed class RoutesSmokeTests : IClassFixture<CookingTimeAppFactory>
{
    private readonly CookingTimeAppFactory _factory;

    public RoutesSmokeTests(CookingTimeAppFactory factory) => _factory = factory;

    public static IEnumerable<object[]> Routes() => new[]
    {
        new object[] { "/" },
        new object[] { "/cooking-time" },
        new object[] { "/about" },
        new object[] { "/help" },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task RazorRoute_Returns200_AndSpaShellAsync(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "MapFallbackToFile returns the SPA shell for every route");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<base href=\"/\"",
            because: "the SPA shell must set the base href so deep-linking works");
        html.Should().Contain("_framework/blazor.webassembly.js",
            because: "the .NET 10 Blazor WebAssembly SDK emits blazor.webassembly.js as the boot script; referencing any other name returns 404 and the WASM payload never bootstraps");
        html.Should().NotContain("_framework/blazor.web.js\"",
            because: "blazor.web.js is the Blazor Server / .NET 6 boot script and is not emitted by the standalone Blazor WebAssembly SDK");
    }

    [Fact]
    public async Task HelpStaticFile_IsServedAsync()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/help/Calculatinghelp.htm");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Calculating", because: "the legacy help file body should be served as-is");
    }

    [Fact]
    public async Task UnknownRoute_FallsBackToSpaIndex_Returns200()
    {
        // This is intentional SPA behavior — MapFallbackToFile("index.html")
        // returns the empty SPA shell for any unknown path so the client-side
        // Router can resolve it. Locking the behavior here so future readers
        // don't mistake it for a bug.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/some-route-that-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<base href=\"/\"", because: "the SPA index sets the base href so deep-linking works");
    }
}

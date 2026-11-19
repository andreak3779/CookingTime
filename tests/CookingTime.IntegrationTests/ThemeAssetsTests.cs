// filepath: tests/CookingTime.IntegrationTests/ThemeAssetsTests.cs
namespace CookingTime.IntegrationTests;

/// <summary>
/// Phase 12: guards the theme assets wired into the served SPA shell.
///
/// The theme is applied by <c>wwwroot/index.html</c> linking
/// <c>css/cookingtime.css</c>, the icon set under <c>icons/</c>, the
/// self-hosted fonts under <c>fonts/</c>, and the blocking
/// <c>js/theme.js</c> script.
///
/// These tests assert against the <em>served</em> HTML and the
/// <em>served</em> assets, because a broken asset link only shows up in a
/// browser - the server happily returns 200 for the shell no matter which
/// stylesheet it points at.
/// </summary>
public sealed class ThemeAssetsTests : IClassFixture<CookingTimeAppFactory>
{
    private readonly CookingTimeAppFactory _factory;

    public ThemeAssetsTests(CookingTimeAppFactory factory) => _factory = factory;

    [Fact]
    public async Task RootIndex_LinksTheThemeStylesheetAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("css/cookingtime.css",
            because: "the theme stylesheet replaces the old css/app.css");
        html.Should().NotContain("css/app.css",
            because: "app.css was deleted in Phase 12 - the theme stylesheet replaces it");
    }

    [Fact]
    public async Task RootIndex_DoesNotReferenceExternalFontCdnAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().NotContain("fonts.googleapis.com",
            because: "the fonts are self-hosted under wwwroot/fonts so the app renders offline");
        html.Should().NotContain("fonts.gstatic.com",
            because: "the fonts are self-hosted under wwwroot/fonts so the app renders offline");
    }

    [Fact]
    public async Task RootIndex_LinksTheIconSetAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("icons/icon.svg",
            because: "the SVG icon is the primary favicon");
        html.Should().Contain("icons/icon-32.png",
            because: "browsers that do not render SVG favicons need a PNG fallback");
    }

    [Fact]
    public async Task RootIndex_DoesNotReferenceTheMissingFaviconIcoAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().NotContain("favicon.ico",
            because: "wwwroot/favicon.ico does not exist - it 404'd on every page load before Phase 12");
    }

    [Fact]
    public async Task RootIndex_LoadsTheThemeScript_BlockingInHeadAsync()
    {
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        html.Should().Contain("js/theme.js",
            because: "the persisted theme must be applied before the first paint");

        // The script must not be deferred: a deferred or end-of-body script
        // would let the browser paint the light theme first, producing a
        // visible flash for users who chose dark mode.
        var headEnd = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var scriptIndex = html.IndexOf("js/theme.js", StringComparison.OrdinalIgnoreCase);

        scriptIndex.Should().BeGreaterThan(-1);
        scriptIndex.Should().BeLessThan(headEnd,
            because: "theme.js must load in <head> so the theme is applied before first paint");
        html.ToLowerInvariant().Should().NotContain("defer",
            because: "a deferred theme script would let the browser paint the light theme first");
    }

    [Theory]
    [InlineData("/css/cookingtime.css")]
    [InlineData("/icons/icon.svg")]
    [InlineData("/icons/icon-32.png")]
    [InlineData("/icons/icon-192.png")]
    [InlineData("/js/theme.js")]
    [InlineData("/fonts/figtree-latin.woff2")]
    [InlineData("/fonts/figtree-latin-ext.woff2")]
    [InlineData("/fonts/bricolage-latin.woff2")]
    [InlineData("/fonts/bricolage-latin-ext.woff2")]
    public async Task ThemeAsset_IsServedAsync(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"the theme asset '{path}' is referenced by the app and must resolve");
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty(
            because: $"the theme asset '{path}' must not be an empty file");
    }

    [Fact]
    public async Task ThemeStylesheet_ServesSelfHostedFontFacesAsync()
    {
        using var client = _factory.CreateClient();

        var css = await client.GetStringAsync("/css/cookingtime.css");

        css.Should().Contain("@font-face",
            because: "the fonts are self-hosted via @font-face rules");
        css.Should().Contain("fonts/figtree-latin.woff2",
            because: "the Figtree latin subset is referenced relatively so it resolves under the app base");
        css.Should().Contain("fonts/bricolage-latin.woff2",
            because: "the Bricolage Grotesque latin subset is referenced relatively");
        css.Should().Contain("font-weight: 400 700",
            because: "these are variable fonts - a weight RANGE is required, otherwise the browser synthesises the missing weights");
        css.Should().Contain("font-weight: 500 800",
            because: "Bricolage Grotesque is a variable font with a wght axis");
    }

    [Fact]
    public async Task ThemeStylesheet_DefinesTheDarkPaletteAsync()
    {
        using var client = _factory.CreateClient();

        var css = await client.GetStringAsync("/css/cookingtime.css");

        css.Should().Contain("[data-theme=\"dark\"]",
            because: "the dark palette is driven by a data-theme attribute on <html>");
    }
}

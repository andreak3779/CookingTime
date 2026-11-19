// filepath: tests/CookingTime.IntegrationTests/ThemeAssetsTests.cs
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Regression guard for the 404s on <c>bricolage-latin.woff2</c> and
    /// <c>figtree-latin.woff2</c>.
    ///
    /// A CSS <c>url()</c> resolves relative to the <em>stylesheet's</em> URL,
    /// not to the document. <c>css/cookingtime.css</c> is served from
    /// <c>/css/</c>, so <c>url('fonts/x.woff2')</c> resolves to
    /// <c>/css/fonts/x.woff2</c> - a directory that is not served. The fonts
    /// live at <c>/fonts/</c>, so the stylesheet must reference
    /// <c>url('../fonts/x.woff2')</c>.
    ///
    /// This test resolves every font URL the way a browser does and asserts
    /// the result is actually served, so a path regression fails here instead
    /// of silently 404-ing in the browser.
    /// </summary>
    [Fact]
    public async Task ThemeStylesheet_ResolvesEveryFontUrlAsync()
    {
        using var client = _factory.CreateClient();

        var css = await client.GetStringAsync("/css/cookingtime.css");

        // The stylesheet's own URL, used as the base for relative resolution.
        const string stylesheetUrl = "/css/cookingtime.css";

        var fontUrls = Regex.Matches(css, @"url\(['""]?(?<path>[^)'""]+\.woff2)['""]?\)")
            .Select(m => m.Groups["path"].Value)
            .ToList();

        fontUrls.Should().HaveCountGreaterThan(0,
            because: "the theme stylesheet must declare @font-face rules pointing at the self-hosted fonts");

        foreach (var fontUrl in fontUrls)
        {
            var resolved = ResolveRelativeTo(stylesheetUrl, fontUrl);

            var response = await client.GetAsync(resolved);

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                because: $"the stylesheet references '{fontUrl}', which a browser resolves to " +
                          $"'{resolved}' - that path must be served, or the font 404s and the " +
                          "theme falls back to a system font");
        }
    }

    /// <summary>
    /// Regression guard for the blank theme button.
    ///
    /// The served <c>js/theme.js</c> used to end with a stray closing
    /// <c>&lt;/content&gt;</c> tag. That made the whole file fail to parse
    /// ("Uncaught SyntaxError: Unexpected token '&lt;'"), so
    /// <c>window.cookingTimeTheme</c> was never defined and the
    /// <c>ThemeToggle</c> interop call threw
    /// "Could not find 'cookingTimeTheme.isDark'".
    ///
    /// The script is served verbatim, so the defect is visible in the served
    /// body. Asserting the interop API is present and that no HTML tag
    /// leaked into the file catches a re-introduction of either problem.
    /// </summary>
    [Fact]
    public async Task ThemeScript_IsSyntacticallyClean_AndExposesInteropApiAsync()
    {
        using var client = _factory.CreateClient();

        var js = await client.GetStringAsync("/js/theme.js");

        js.Should().NotContain("</",
            because: "a stray closing tag makes the browser fail to parse the whole script, " +
                      "so window.cookingTimeTheme is never defined and the theme toggle throws " +
                      "'Could not find cookingTimeTheme.isDark'");

        js.Should().Contain("window.cookingTimeTheme",
            because: "the ThemeToggle component resolves 'cookingTimeTheme.isDark' and " +
                      "'cookingTimeTheme.toggle' through IJSRuntime");
        js.Should().Contain("isDark",
            because: "ThemeToggle reads the live theme through cookingTimeTheme.isDark");
        js.Should().Contain("toggle",
            because: "ThemeToggle flips the theme through cookingTimeTheme.toggle");
    }

    /// <summary>
    /// Resolves <paramref name="relativePath"/> the way a browser resolves a
    /// CSS <c>url()</c>: against the directory containing
    /// <paramref name="baseUrl"/>, with <c>.</c> and <c>..</c> applied.
    /// </summary>
    private static string ResolveRelativeTo(string baseUrl, string relativePath)
    {
        var baseSegments = baseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var baseDirectory = baseSegments[..^1].ToList();

        foreach (var segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (segment)
            {
                case ".":
                    continue;
                case "..":
                    if (baseDirectory.Count > 0)
                    {
                        baseDirectory.RemoveAt(baseDirectory.Count - 1);
                    }

                    continue;
                default:
                    baseDirectory.Add(segment);
                    break;
            }
        }

        return "/" + string.Join('/', baseDirectory);
    }
}

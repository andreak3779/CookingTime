// filepath: tests/CookingTime.ComponentTests/ThemeToggleRazorTests.cs
using Microsoft.AspNetCore.Components.Web;

namespace CookingTime.ComponentTests;

/// <summary>
/// Component-level tests for the Phase 12 <see cref="Layout.ThemeToggle"/>.
///
/// The toggle is a thin Blazor wrapper over <c>wwwroot/js/theme.js</c>: it
/// asks JavaScript whether dark mode is currently applied, and calls
/// <c>cookingTimeTheme.toggle()</c> when the user clicks. These tests pin the
/// interop contract - the JS identifiers and their arguments - so a rename in
/// either direction fails here rather than silently at runtime.
///
/// The JavaScript itself is exercised by the browser, not by bUnit; bUnit's
/// test renderer has no DOM and no <c>window</c>.
/// </summary>
public sealed class ThemeToggleRazorTests : TestContext
{
    private const string IsDarkIdentifier = "cookingTimeTheme.isDark";
    private const string ToggleIdentifier = "cookingTimeTheme.toggle";

    [Fact]
    public void RendersToggleButton_WithThemeClasses()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(true);

        var ctx = RenderComponent<Layout.ThemeToggle>();
        var button = ctx.Find("button");

        button.ClassList.Should().Contain("ct-theme-toggle");
        button.GetAttribute("type").Should().Be("button");
    }

    [Fact]
    public void ReadsCurrentTheme_FromJavaScript_OnFirstRender()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(true);

        var ctx = RenderComponent<Layout.ThemeToggle>();

        ctx.WaitForAssertion(() => JSInterop.Invocations[IsDarkIdentifier].Count.Should().Be(1,
            because: "the component asks JavaScript what the live DOM state is"));

        ctx.Find("button").GetAttribute("aria-pressed").Should().Be("true",
            because: "aria-pressed reflects the state JavaScript reported");
    }

    [Fact]
    public void AriaPressed_IsFalse_WhenLightThemeIsActive()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(false);

        var ctx = RenderComponent<Layout.ThemeToggle>();

        ctx.WaitForAssertion(() =>
            ctx.Find("button").GetAttribute("aria-pressed").Should().Be("false"));
    }

    [Fact]
    public async Task ClickingToggle_CallsThemeJs_ToggleAsync()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(false);
        JSInterop.Setup<string>(ToggleIdentifier).SetResult("dark");

        var ctx = RenderComponent<Layout.ThemeToggle>();
        ctx.WaitForAssertion(() => JSInterop.Invocations[IsDarkIdentifier].Count.Should().Be(1));

        await ctx.Find("button").ClickAsync(new MouseEventArgs());

        JSInterop.Invocations[ToggleIdentifier].Count.Should().Be(1,
            because: "clicking the toggle delegates the flip to theme.js");
    }

    [Fact]
    public async Task ClickingToggle_FlipsAriaPressedAsync()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(false);
        JSInterop.Setup<string>(ToggleIdentifier).SetResult("dark");

        var ctx = RenderComponent<Layout.ThemeToggle>();
        ctx.WaitForAssertion(() =>
            ctx.Find("button").GetAttribute("aria-pressed").Should().Be("false"));

        await ctx.Find("button").ClickAsync(new MouseEventArgs());

        ctx.WaitForAssertion(() =>
            ctx.Find("button").GetAttribute("aria-pressed").Should().Be("true",
                because: "theme.js returns the newly applied theme, which the component reflects"));
    }

    [Fact]
    public void ToggleButton_HasAccessibleLabel()
    {
        JSInterop.Setup<bool>(IsDarkIdentifier).SetResult(false);

        var ctx = RenderComponent<Layout.ThemeToggle>();
        var button = ctx.Find("button");

        button.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace(
            because: "an icon-only control needs an accessible name");
    }
}

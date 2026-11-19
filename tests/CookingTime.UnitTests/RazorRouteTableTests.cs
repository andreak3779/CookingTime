// filepath: tests/CookingTime.UnitTests/RazorRouteTableTests.cs
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace CookingTime.UnitTests;

/// <summary>
/// Guards the client-side route table.
///
/// Phase 12 found that a reference design package placed under the project
/// root was being compiled into the app, producing a second component for
/// every route. Blazor's <c>RouteTableFactory.DetectAmbiguousRoutes</c> then
/// throws <c>InvalidOperationException: The following routes are ambiguous</c>
/// when the WASM router builds its route table — in the browser, after the
/// payload boots.
///
/// Every other guard in this repo missed it:
///   - the build stays green (a duplicate route is not a compile error),
///   - the server returns HTTP 200 for every route (it serves the SPA shell),
///   - and no server-side test exercises the client-side route table.
///
/// These facts are the automated proxy for that runtime invariant. They assert
/// the same thing Blazor asserts: exactly one component per route template.
/// See docs/decisions/0007-design-package-as-reference-artifact.md.
/// </summary>
public sealed class RazorRouteTableTests
{
    /// <summary>
    /// Every routable component in the app assembly, with the route templates
    /// it declares. Mirrors <c>RouteTableFactory.GetRouteableComponents</c>:
    /// a type qualifies when it is an <see cref="IComponent"/> and carries a
    /// <see cref="RouteAttribute"/>.
    /// </summary>
    private static IReadOnlyList<(Type Component, string Template)> GetRoutableComponents()
    {
        var assembly = typeof(Program).Assembly;

        return assembly.ExportedTypes
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type
                .GetCustomAttributes<RouteAttribute>(inherit: false)
                .Select(route => (Component: type, Template: route.Template)))
            .ToList();
    }

    [Fact]
    public void EveryRouteTemplate_IsClaimedByExactlyOneComponent()
    {
        // Arrange
        var routes = GetRoutableComponents();

        // Act
        var duplicates = routes
            .GroupBy(r => r.Template, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(r => r.Component).Distinct().Count() > 1)
            .Select(g => string.Join(", ", g.Select(r => r.Component.FullName)))
            .ToList();

        // Assert
        duplicates.Should().BeEmpty(
            because: "Blazor's DetectAmbiguousRoutes throws when two components claim the same route; " +
                     "the duplicate route only surfaces in the browser at router-boot time, so this test is the only " +
                     "automated guard");
    }

    [Fact]
    public void ExpectedRoutes_AreEachDeclaredExactlyOnce()
    {
        // Arrange
        var routes = GetRoutableComponents();

        // Assert
        foreach (var template in new[] { "/", "/cooking-time", "/help", "/about" })
        {
            routes.Count(r => string.Equals(r.Template, template, StringComparison.OrdinalIgnoreCase))
                .Should().Be(1,
                    because: $"the route '{template}' must be declared by exactly one component");
        }
    }

    [Fact]
    public void RoutableComponents_AreOnlyTheKnownPages()
    {
        // Arrange
        var routes = GetRoutableComponents();

        // Act
        var components = routes
            .Select(r => r.Component)
            .Distinct()
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Assert
        // Any component here outside the known page set means something new
        // was compiled into the app that nobody intended to ship.
        components.Should().BeEquivalentTo(new[]
        {
            "CookingTime.Pages.About",
            "CookingTime.Pages.CookingTime",
            "CookingTime.Pages.Help",
            "CookingTime.Pages.Index",
        });
    }

    [Fact]
    public void NoComponent_LivesUnderTheDocsOrDesignFolders()
    {
        // Arrange
        var routes = GetRoutableComponents();

        // Assert
        routes.Should().NotContain(r =>
                r.Component.FullName!.Contains("design", StringComparison.OrdinalIgnoreCase),
            because: "reference design packages are artifacts under docs/design/, not app components");
    }
}

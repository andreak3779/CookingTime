// filepath: tests/CookingTime.IntegrationTests/CookingTimeAppFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CookingTime.IntegrationTests;

/// <summary>
/// One in-process WebApplicationFactory per test class. Spins up the
/// Phase 5 test-only host on top of TestServer, with the same
/// <c>appsettings.json</c> the WASM client ships with, so
/// <c>IOptions&lt;MealCatalogOptions&gt;</c> and
/// <c>IOptions&lt;ProductInfoOptions&gt;</c> resolve identically in
/// both runtimes.
/// </summary>
public sealed class CookingTimeAppFactory : WebApplicationFactory<Marker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // WebApplicationFactory resolves the content root from the test
        // runner's CWD, which is the repo root when running through
        // `dotnet test`. Override it to the test assembly's bin directory
        // so appsettings.json and the WASM wwwroot are picked up.
        builder.UseContentRoot(AppContext.BaseDirectory);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        });
    }
}

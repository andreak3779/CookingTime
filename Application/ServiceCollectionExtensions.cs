// filepath: Application/ServiceCollectionExtensions.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Services;
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using CookingTime.Infrastructure.Meals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CookingTime.Application;

/// <summary>
/// Single source of truth for the CookingTime composition root.
/// Both the Blazor WebAssembly client (<c>Program.cs</c>) and the
/// Phase 5 test-only host (<c>tests/CookingTime.IntegrationTests</c>)
/// call <see cref="AddCookingTime"/>; any new application service
/// belongs here, not in either entry point.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the production CookingTime services: options binding
    /// from <paramref name="configuration"/>, the meal factory, and the
    /// cooking-time calculator. Pure composition — no Web, no logging.
    /// </summary>
    public static IServiceCollection AddCookingTime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MealCatalogOptions>(
            configuration.GetSection(MealCatalogOptions.SectionName));
        services.Configure<ProductInfoOptions>(
            configuration.GetSection(ProductInfoOptions.SectionName));

        // Stateless; WASM has no per-request scope and the test host
        // is a single-process integration fixture. Safe as Singleton.
        services.AddSingleton<IMealFactory, MealFactory>();
        services.AddSingleton<ICookingTimeCalculator, CookingTimeCalculator>();

        return services;
    }
}

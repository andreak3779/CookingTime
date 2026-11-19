// filepath: Application/ServiceCollectionExtensions.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Services;
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using CookingTime.Infrastructure.Meals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // Phase 15: bind the options AND validate them at startup.
        //
        // Previously both sections were bound with a plain Configure<T>, which
        // validates nothing. A malformed catalog entry then failed late and
        // in the wrong place: MealFactory throws from its constructor, the
        // factory is resolved lazily when the UI first asks for the catalog,
        // and the browser shows an unhandled component exception with no hint
        // about which config line was wrong.
        //
        // ValidateOnStart moves that failure to startup, where the message can
        // name the offending entry. The validators live in
        // Infrastructure/Configuration/OptionsValidators.cs.
        services
            .AddOptions<MealCatalogOptions>()
            .Bind(configuration.GetSection(MealCatalogOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<ProductInfoOptions>()
            .Bind(configuration.GetSection(ProductInfoOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MealCatalogOptions>, MealCatalogOptionsValidator>();
        services.AddSingleton<IValidateOptions<ProductInfoOptions>, ProductInfoOptionsValidator>();

        // Stateless; WASM has no per-request scope and the test host
        // is a single-process integration fixture. Safe as Singleton.
        services.AddSingleton<IMealFactory, MealFactory>();
        services.AddSingleton<ICookingTimeCalculator, CookingTimeCalculator>();

        return services;
    }
}

// filepath: tests/CookingTime.UnitTests/Application/ServiceCollectionExtensionsTests.cs
using CookingTime.Application;
using CookingTime.Application.Abstractions;
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using CookingTime.Infrastructure.Meals;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CookingTime.UnitTests.Application;

/// <summary>
/// Phase 7: AddCookingTime is the single composition root for the WASM
/// client (Program.cs) and the Phase 5 test-only host. This test
/// asserts the wiring without standing up a WebApplication, so the
/// service-registration code path is covered by the canonical UnitTests
/// project. Before this test, the AddCookingTime extension had 0% line
/// coverage in UnitTests.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCookingTime_RegistersAllProductionServices()
    {
        // Minimal configuration that satisfies both options sections.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Meals:Catalog:0:Kind"] = "Range",
                ["Meals:Catalog:0:Name"] = "Test",
                ["Meals:Catalog:0:Instructions"] = "Test",
                ["Meals:Catalog:0:MinPounds"] = "1",
                ["Meals:Catalog:0:MaxPounds"] = "10",
                ["Meals:Catalog:0:MinMinutesPerPound"] = "10",
                ["Meals:Catalog:0:MaxMinutesPerPound"] = "20",
                ["ProductInfo:Name"] = "CookingTime",
                ["ProductInfo:Version"] = "1.0.0",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCookingTime(config);
        using var provider = services.BuildServiceProvider();

        provider.GetService<IMealFactory>().Should().NotBeNull()
            .And.BeOfType<MealFactory>();
        provider.GetService<ICookingTimeCalculator>().Should().NotBeNull();

        var mealOptions = provider.GetRequiredService<IOptions<MealCatalogOptions>>();
        mealOptions.Value.Catalog.Should().HaveCount(1);
        mealOptions.Value.Catalog[0].Name.Should().Be("Test");

        var productOptions = provider.GetRequiredService<IOptions<ProductInfoOptions>>();
        productOptions.Value.Name.Should().Be("CookingTime");
    }

    [Fact]
    public void AddCookingTime_NullServices_Throws()
    {
        var config = new ConfigurationBuilder().Build();
        var act = () => ((IServiceCollection)null!).AddCookingTime(config);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddCookingTime_NullConfiguration_Throws()
    {
        var services = new ServiceCollection();
        var act = () => services.AddCookingTime(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void AddCookingTime_InvalidCatalog_FailsAtStartup_NotAtFirstUse()
    {
        // Phase 15: the catalog used to be bound with a plain Configure<T>,
        // which validated nothing. A malformed entry then failed lazily -
        // MealFactory throws from its constructor, and the factory is
        // resolved the first time the UI asks for the catalog, so the browser
        // showed an unhandled component exception with no hint about which
        // config line was wrong. ValidateOnStart moves that to startup.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // MaxMinutesPerPound below MinMinutesPerPound - an inverted range.
                ["Meals:Catalog:0:Kind"] = "Range",
                ["Meals:Catalog:0:Name"] = "Bad Meal",
                ["Meals:Catalog:0:Instructions"] = "Test",
                ["Meals:Catalog:0:MinPounds"] = "1",
                ["Meals:Catalog:0:MaxPounds"] = "10",
                ["Meals:Catalog:0:MinMinutesPerPound"] = "45",
                ["Meals:Catalog:0:MaxMinutesPerPound"] = "40",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCookingTime(config);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<MealCatalogOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*less than MinMinutesPerPound*");
    }

    [Fact]
    public void AddCookingTime_InvalidResumeUrl_FailsAtStartup()
    {
        // Phase 15: a javascript: ResumeUrl would be rendered into an href.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProductInfo:ResumeUrl"] = "javascript:alert(1)",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCookingTime(config);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<ProductInfoOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*absolute http/https URI*");
    }

    [Fact]
    public void AddCookingTime_ValidCatalog_PassesValidation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Meals:Catalog:0:Kind"] = "Range",
                ["Meals:Catalog:0:Name"] = "Good Meal",
                ["Meals:Catalog:0:Instructions"] = "Test",
                ["Meals:Catalog:0:MinPounds"] = "1",
                ["Meals:Catalog:0:MaxPounds"] = "10",
                ["Meals:Catalog:0:MinMinutesPerPound"] = "40",
                ["Meals:Catalog:0:MaxMinutesPerPound"] = "45",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddCookingTime(config);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<MealCatalogOptions>>().Value;

        act.Should().NotThrow();
    }
}
// filepath: tests/CookingTime.IntegrationTests/CookingTimeFeatureTests.cs
using Bunit;
using CookingTime.Application;
using CookingTime.Application.Abstractions;
using CookingTime.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CookingTimePage = CookingTime.Pages.CookingTime;

namespace CookingTime.IntegrationTests;

/// <summary>
/// End-to-end bUnit feature tests that exercise the real
/// <see cref="CookingTimePage"/> page through the production
/// <see cref="ServiceCollectionExtensions.AddCookingTime"/> composition
/// root. No mocks on the calculator path — the point is to verify the
/// integration seam (DI + IOptions + the form's submit hook) holds
/// together.
/// </summary>
public sealed class CookingTimeFeatureTests : TestContext
{
    public CookingTimeFeatureTests()
    {
        // Reuse the appsettings.json from the WASM client so IOptions
        // resolve the same values as production.
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        Services.AddCookingTime(config);
    }

    [Fact]
    public async Task Submit_ValidModel_RendersResultAndInstructionsAsync()
    {
        // Pass InitialModel at first render so OnInitialized picks it up.
        var component = RenderComponent<CookingTimePage>(parameters => parameters
            .Add(p => p.InitialModel, new CookingTimePage.CookingTimeViewModel
            {
                Meal = "Chicken",
                Weight = 4.0m,
                Unit = WeightUnit.Pounds,
            }));

        await component.InvokeAsync(() => component.Instance.SubmitAsync());
        component.Render(); // flush the post-await re-render

        component.Markup.Should().Contain("<h2>Cooking Time</h2>",
            because: "the result section heading is rendered after a successful submit");
        component.Markup.Should().Contain("Cooking Instructions",
            because: "the result section includes instructions");
        component.Markup.Should().NotContain("Please select a meal",
            because: "a valid model should not surface validation errors");
    }

    [Fact]
    public async Task Submit_DefaultModel_FailsValidation_RendersErrorsAsync()
    {
        var component = RenderComponent<CookingTimePage>();

        // No InitialModel — the underlying _model has the default empty Meal.
        await component.InvokeAsync(() => component.Instance.SubmitAsync());
        component.Render(); // flush the post-await re-render

        component.Markup.Should().Contain("Please select a meal",
            because: "the calculator rejects the empty Meal default");
        component.Markup.Should().Contain("validation-errors",
            because: "the failure result renders the validation-errors list");
    }

    [Fact]
    public void MealCatalog_FromProductionConfig_IncludesEveryConfiguredMeal()
    {
        // Regression test: MealTypeRegistry used to key factories by MealKind,
        // so every configured MealKind.Range meal but the last one silently
        // overwrote its predecessor. This resolves the real appsettings.json
        // through the production composition root and asserts all of them
        // (not just the last-registered one) survive into the catalog.
        using var provider = Services.BuildServiceProvider();
        var mealFactory = provider.GetRequiredService<IMealFactory>();

        var meals = mealFactory.CreateAll();

        meals.Should().HaveCount(12,
            because: "2 code-baked meals (Chicken, Turkey) + 10 Range meals configured in appsettings.json");
        meals.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "Chicken",
            "Turkey",
            "Pork Roast Loin, Leg, Butt",
            "Beef Roast Standing Rib - Rare",
            "Beef Roast Standing Rib - Medium",
            "Beef Roast Rolled - Rare",
            "Beef Roast Rolled - Medium",
            "Beef Roast Round or Rump - Rare",
            "Beef Roast Round or Rump - Medium",
            "Smoked Ham - Whole",
            "Smoked Ham - Half",
            "Pork - Smoked Picnic Shoulder",
        });
    }

    [Fact]
    public async Task Submit_ConfigDrivenMeal_ResolvesItByNameAsync()
    {
        // Phase 17: the calculator now asks the factory for a meal by name
        // instead of rebuilding the catalog on every calculation. This
        // exercises that path end to end with a REAL configured meal (not a
        // code-baked one) through the production composition root.
        var component = RenderComponent<CookingTimePage>(parameters => parameters
            .Add(p => p.InitialModel, new CookingTimePage.CookingTimeViewModel
            {
                Meal = "Pork Roast Loin, Leg, Butt",
                Weight = 5.0m,
                Unit = WeightUnit.Pounds,
            }));

        await component.InvokeAsync(() => component.Instance.SubmitAsync());
        component.Render();

        component.Markup.Should().Contain("<h2>Cooking Time</h2>",
            because: "the config-driven meal must resolve and produce a result");
        component.Markup.Should().Contain("3 hrs. and 20 mins.",
            because: "5 lb at 40 minutes per pound is 200 minutes");
    }

    [Fact]
    public async Task Submit_ConfigDrivenMeal_OutOfRangeWeight_ReturnsValidationErrorAsync()
    {
        // Phase 14 + 17 together, through the real config: a config-driven
        // meal must enforce its own weight range, exactly as the code-baked
        // ones do.
        var component = RenderComponent<CookingTimePage>(parameters => parameters
            .Add(p => p.InitialModel, new CookingTimePage.CookingTimeViewModel
            {
                Meal = "Pork Roast Loin, Leg, Butt",
                Weight = 500.0m,
                Unit = WeightUnit.Pounds,
            }));

        await component.InvokeAsync(() => component.Instance.SubmitAsync());
        component.Render();

        component.Markup.Should().Contain("outside the supported range",
            because: "the configured meal's max is 24 lb, so 500 lb must be rejected");
    }

    [Fact]
    public void MealLookup_FindsEveryConfiguredMeal_ByName()
    {
        // Phase 17: the name index must cover every meal in the production
        // catalog, not just the code-baked ones.
        using var provider = Services.BuildServiceProvider();
        var mealFactory = provider.GetRequiredService<IMealFactory>();

        foreach (var meal in mealFactory.CreateAll())
        {
            mealFactory.TryGetByName(meal.Name, out var found).Should().BeTrue(
                because: $"'{meal.Name}' is in the catalog and must be findable by name");
            found.Should().NotBeNull();
        }
    }
}

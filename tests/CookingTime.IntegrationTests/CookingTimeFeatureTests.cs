// filepath: tests/CookingTime.IntegrationTests/CookingTimeFeatureTests.cs
using Bunit;
using CookingTime.Application;
using CookingTime.Application.Abstractions;
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
}

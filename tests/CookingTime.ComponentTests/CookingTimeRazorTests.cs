// filepath: tests/CookingTime.ComponentTests/CookingTimeRazorTests.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Common;
using CookingTime.ComponentTests.Support;
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;
using CookingTime.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CookingTime.ComponentTests;

/// <summary>
/// Component-level tests for <see cref="Pages.CookingTime"/>.
///
/// Submit-and-assert paths are exercised by <c>CookingTimeCalculatorTests</c>
/// at the unit level. The bUnit suite here focuses on what the bUnit renderer
/// can verify reliably: rendered structure, meal-option population, and
/// DI plumbing. We avoid the flaky InputSelect/InputNumber binding in bUnit's
/// test renderer and use <c>InitialModel</c> + <c>SubmitAsync</c> for the
/// interactions we do cover.
/// </summary>
public sealed class CookingTimeRazorTests : TestContext
{
    private readonly Mock<ICookingTimeCalculator> _calculator = new();

    private static IMealFactory BuildFactory(params IMeal[] meals)
    {
        var mock = new Mock<IMealFactory>();
        mock.Setup(f => f.CreateAll()).Returns(meals);
        return mock.Object;
    }

    private void ConfigureServices(IMealFactory factory)
    {
        Services.AddSingleton<IMealFactory>(factory);
        Services.AddSingleton(_calculator.Object);
        Services.Configure<MealCatalogOptions>(o => o.Catalog = new());
    }

    [Fact]
    public void RendersMealOptions_FromFactory()
    {
        ConfigureServices(BuildFactory(
            new TestMeal("Chicken", "x", MealKind.Chicken, Weight.FromPounds(1), Weight.FromPounds(10)),
            new TestMeal("Turkey", "x", MealKind.Turkey, Weight.FromPounds(1), Weight.FromPounds(24))));

        var ctx = RenderComponent<Pages.CookingTime>();
        var options = ctx.FindAll("option");

        options.Should().HaveCount(3); // "Select a meal..." + 2 meals
        options.Select(o => o.TextContent).Should().Contain(new[] { "Select a meal...", "Chicken", "Turkey" });
    }

    [Fact]
    public void RendersPoundsAndKilogramsRadios()
    {
        ConfigureServices(BuildFactory(
            new TestMeal("Chicken", "x", MealKind.Chicken, Weight.FromPounds(1), Weight.FromPounds(10))));

        var ctx = RenderComponent<Pages.CookingTime>();
        var radios = ctx.FindAll("input[type=radio]");

        radios.Should().HaveCount(2);
        radios.Select(r => r.GetAttribute("value")).Should().Contain(new[] { "Pounds", "Kilograms" });
    }

    [Fact]
    public void RendersCalculateAndResetButtons()
    {
        ConfigureServices(BuildFactory(
            new TestMeal("Chicken", "x", MealKind.Chicken, Weight.FromPounds(1), Weight.FromPounds(10))));

        var ctx = RenderComponent<Pages.CookingTime>();
        var buttons = ctx.FindAll("button");

        buttons.Should().HaveCount(2);
        buttons[0].TextContent.Should().Contain("Calculate");
        buttons[1].TextContent.Should().Contain("Reset");
    }

    [Fact]
    public async Task Calculator_InvokedOnce_WhenSubmittedWithPreFilledModel()
    {
        ConfigureServices(BuildFactory(
            new TestMeal("Chicken", "x", MealKind.Chicken, Weight.FromPounds(1), Weight.FromPounds(10))));
        _calculator
            .Setup(c => c.CalculateAsync("Chicken", 2M, WeightUnit.Pounds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CookingResult.Success("ok", "inst"));

        var ctx = RenderComponent<Pages.CookingTime>(parameters => parameters
            .Add(p => p.InitialModel, new Pages.CookingTime.CookingTimeViewModel
            {
                Meal = "Chicken",
                Weight = 2M,
                Unit = WeightUnit.Pounds,
            }));

        // Invoke the test hook directly so we don't rely on bUnit's
        // InputSelect/InputNumber change events (flaky in the test
        // renderer). The fact that the calculator is invoked proves the
        // component's wiring (DI, model state, HandleSubmit) works.
        await ctx.Instance.SubmitAsync();

        _calculator.Verify(
            c => c.CalculateAsync("Chicken", 2M, WeightUnit.Pounds, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Submit_WithoutInitialModel_DefaultModel_FailsValidation_NoCalculatorCall()
    {
        ConfigureServices(BuildFactory(
            new TestMeal("Chicken", "x", MealKind.Chicken, Weight.FromPounds(1), Weight.FromPounds(10))));

        var ctx = RenderComponent<Pages.CookingTime>();

        // Default view model has Meal="" (validation blocks submit before
        // the calculator is invoked). Verify the calculator is never called.
        ctx.Find("form").Submit();

        _calculator.Verify(
            c => c.CalculateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<WeightUnit>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

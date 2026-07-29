// filepath: tests/CookingTime.UnitTests/Infrastructure/MealFactoryTests.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using CookingTime.Infrastructure.Meals;
using Microsoft.Extensions.Options;

namespace CookingTime.UnitTests.Infrastructure;

public sealed class MealFactoryTests
{
    [Fact]
    public void CreateAll_ReturnsChickenAndTurkey_RegardlessOfConfig()
    {
        var factory = new MealFactory(Options.Create(new MealCatalogOptions()));

        var meals = factory.CreateAll();

        meals.Select(m => m.Name).Should().Contain("Chicken").And.Contain("Turkey");
    }

    [Fact]
    public void CreateAll_AddsRangeMealsFromConfig()
    {
        var options = new MealCatalogOptions
        {
            Catalog =
            {
                new MealDefinition
                {
                    Kind = MealKind.Range,
                    Name = "Pork Roast",
                    Instructions = "Cook it.",
                    MinPounds = 1, MaxPounds = 24,
                    MinMinutesPerPound = 40, MaxMinutesPerPound = 45,
                },
            },
        };
        var factory = new MealFactory(Options.Create(options));

        var meals = factory.CreateAll();

        meals.Select(m => m.Name).Should().Contain("Pork Roast");
    }

    [Fact]
    public void CreateAll_RangeMealMissingMaxMinutes_ThrowsOnCreation()
    {
        var options = new MealCatalogOptions
        {
            Catalog =
            {
                new MealDefinition
                {
                    Kind = MealKind.Range,
                    Name = "Bad Meal",
                    Instructions = "x",
                    MinPounds = 1, MaxPounds = 10,
                    MinMinutesPerPound = 30,
                    MaxMinutesPerPound = null,
                },
            },
        };
        var factory = new MealFactory(Options.Create(options));

        var act = () => factory.CreateAll();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Bad Meal*must specify both*");
    }

    [Fact]
    public void CreateAll_NullOptions_Throws()
    {
        var act = () => new MealFactory((IOptions<MealCatalogOptions>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateAll_ChickenInConfig_DoesNotDuplicate()
    {
        // Even if the config tries to register a Chicken entry, the
        // factory skips it (the code-baked Chicken is the canonical source).
        var options = new MealCatalogOptions
        {
            Catalog =
            {
                new MealDefinition
                {
                    Kind = MealKind.Chicken,
                    Name = "Chicken (config attempt)",
                    Instructions = "x",
                    MinPounds = 1, MaxPounds = 6,
                },
            },
        };
        var factory = new MealFactory(Options.Create(options));

        var meals = factory.CreateAll();
        meals.Count(m => m.Name == "Chicken").Should().Be(1);
    }
}

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
    public void CreateAll_MultipleRangeMealsInConfig_ReturnsAllOfThem()
    {
        // Regression test: every configured roast/ham shares MealKind.Range, so
        // the registry must not let later registrations overwrite earlier ones.
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
                new MealDefinition
                {
                    Kind = MealKind.Range,
                    Name = "Beef Roast",
                    Instructions = "Cook it.",
                    MinPounds = 1, MaxPounds = 24,
                    MinMinutesPerPound = 18, MaxMinutesPerPound = 20,
                },
                new MealDefinition
                {
                    Kind = MealKind.Range,
                    Name = "Smoked Ham",
                    Instructions = "Cook it.",
                    MinPounds = 1, MaxPounds = 24,
                    MinMinutesPerPound = 15, MaxMinutesPerPound = 18,
                },
            },
        };
        var factory = new MealFactory(Options.Create(options));

        var meals = factory.CreateAll();

        meals.Select(m => m.Name).Should().Contain(new[] { "Pork Roast", "Beef Roast", "Smoked Ham" });
        meals.Count(m => m.Kind == MealKind.Range).Should().Be(3);
    }

    [Fact]
    public void CreateAll_RangeMealMissingMaxMinutes_ThrowsOnCreation()
    {
        // Phase 17: the catalog is materialised in the constructor, so a
        // malformed entry fails when the factory is built rather than when
        // the catalog is first read. That is the earlier, clearer failure
        // point - and it is now also the Phase 15 startup validation's job
        // to catch it before the app runs at all.
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

        var act = () => new MealFactory(Options.Create(options));

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

    [Fact]
    public void TryGetByName_FindsTheMeal_CaseInsensitively()
    {
        // Phase 17: the calculator asks the factory by name instead of
        // rebuilding the catalog on every calculation.
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

        factory.TryGetByName("Pork Roast", out var exact).Should().BeTrue();
        exact.Should().NotBeNull();
        exact!.Name.Should().Be("Pork Roast");

        factory.TryGetByName("pork roast", out var lower).Should().BeTrue();
        lower!.Name.Should().Be("Pork Roast");

        factory.TryGetByName("PORK ROAST", out var upper).Should().BeTrue();
        upper!.Name.Should().Be("Pork Roast");
    }

    [Fact]
    public void TryGetByName_UnknownName_ReturnsFalseAndNull()
    {
        var factory = new MealFactory(Options.Create(new MealCatalogOptions()));

        factory.TryGetByName("Nonexistent Meal", out var meal).Should().BeFalse();
        meal.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetByName_BlankName_ReturnsFalseAndNull(string name)
    {
        var factory = new MealFactory(Options.Create(new MealCatalogOptions()));

        factory.TryGetByName(name, out var meal).Should().BeFalse();
        meal.Should().BeNull();
    }

    [Fact]
    public void TryGetByName_TrimsSurroundingWhitespace()
    {
        var factory = new MealFactory(Options.Create(new MealCatalogOptions()));

        factory.TryGetByName("  Chicken  ", out var meal).Should().BeTrue();
        meal!.Name.Should().Be("Chicken");
    }

    [Fact]
    public void CreateAll_ReturnsTheSameInstances_OnEveryCall()
    {
        // Phase 17: the catalog is materialised once in the constructor.
        // Re-invoking the factory delegates per call was pure waste - the
        // meals are stateless.
        var factory = new MealFactory(Options.Create(new MealCatalogOptions()));

        var first = factory.CreateAll();
        var second = factory.CreateAll();

        second.Should().HaveSameCount(first);
        for (var i = 0; i < first.Count; i++)
        {
            second[i].Should().BeSameAs(first[i]);
        }
    }
}

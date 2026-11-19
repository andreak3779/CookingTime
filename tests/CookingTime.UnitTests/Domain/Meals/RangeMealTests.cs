// filepath: tests/CookingTime.UnitTests/Domain/Meals/RangeMealTests.cs
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Meals;

public sealed class RangeMealTests
{
    [Fact]
    public void Constructor_BlankName_Throws()
    {
        var act = () => new RangeMeal("", "x", 30, 40, 1M, 10M);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NegativeRangeMin_Throws()
    {
        var act = () => new RangeMeal("x", "y", 30, 40, -1M, 10M);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_MaxLessThanMin_Throws()
    {
        var act = () => new RangeMeal("x", "y", 30, 40, 5M, 4M);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FivePoundsAt30To40PerPound_Yields150To200Minutes()
    {
        var meal = new RangeMeal("Test", "x", 30, 40, 1M, 10M);
        var d = meal.Calculate(Weight.FromPounds(5M));
        d.Minimum.Should().Be(TimeSpan.FromMinutes(150));
        d.Maximum.Should().Be(TimeSpan.FromMinutes(200));
    }

    [Fact]
    public void Calculate_BelowMinimumWeight_Throws()
    {
        // Phase 14 regression: RangeMeal never validated the weight against
        // its own range, so every configured meal accepted any weight. A
        // 0.5 lb pork roast produced a 20-minute cook time and the
        // application layer reported it as a SUCCESS.
        var meal = new RangeMeal("Test", "x", 30, 40, 1M, 10M);

        var act = () => meal.Calculate(Weight.FromPounds(0.5M));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*outside the supported range for Test*");
    }

    [Fact]
    public void Calculate_AboveMaximumWeight_Throws()
    {
        // Phase 14 regression: at 500 lb the linear formula produced a
        // duration whose minimum (21h20m) exceeded its maximum (15h00m),
        // and the app displayed it as a successful result.
        var meal = new RangeMeal("Test", "x", 30, 40, 1M, 10M);

        var act = () => meal.Calculate(Weight.FromPounds(500M));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*outside the supported range for Test*");
    }

    [Fact]
    public void Calculate_AtMinimumWeight_IsAllowed()
    {
        var meal = new RangeMeal("Test", "x", 30, 40, 1M, 10M);
        var act = () => meal.Calculate(Weight.FromPounds(1M));
        act.Should().NotThrow();
    }

    [Fact]
    public void Calculate_AtMaximumWeight_IsAllowed()
    {
        var meal = new RangeMeal("Test", "x", 30, 40, 1M, 10M);
        var act = () => meal.Calculate(Weight.FromPounds(10M));
        act.Should().NotThrow();
    }
}

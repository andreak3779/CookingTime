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
}

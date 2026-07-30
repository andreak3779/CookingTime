// filepath: tests/CookingTime.UnitTests/Domain/Strategies/TurkeyTableStrategyTests.cs
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Strategies;

/// <summary>
/// Validates every TurkeyMeal table row against the expected min/max,
/// plus the boundary edges just below and above the supported range.
/// </summary>
public sealed class TurkeyTableStrategyTests
{
    [Theory]
    // Row 1
    [InlineData("6.0",   3, 45, 4,  0)]
    [InlineData("7.0",   3, 45, 4,  0)]
    [InlineData("8.0",   3, 45, 4,  0)]
    // Row 2
    [InlineData("8.0001", 4,  0, 4, 30)]
    [InlineData("9.0",    4,  0, 4, 30)]
    [InlineData("10.0",   4,  0, 4, 30)]
    // Row 3
    [InlineData("10.001", 4, 30, 5,  0)]
    [InlineData("11.0",   4, 30, 5,  0)]
    [InlineData("12.0",   4, 30, 5,  0)]
    // Row 4
    [InlineData("12.001", 5,  0, 5, 15)]
    [InlineData("13.0",   5,  0, 5, 15)]
    [InlineData("14.0",   5,  0, 5, 15)]
    // Row 5
    [InlineData("14.001", 5, 15, 6,  0)]
    [InlineData("15.0",   5, 15, 6,  0)]
    [InlineData("16.0",   5, 15, 6,  0)]
    // Row 6
    [InlineData("16.001", 6,  0, 6, 30)]
    [InlineData("17.0",   6,  0, 6, 30)]
    [InlineData("18.0",   6,  0, 6, 30)]
    // Row 7
    [InlineData("18.001", 6, 30, 7, 30)]
    [InlineData("19.0",   6, 30, 7, 30)]
    [InlineData("20.0",   6, 30, 7, 30)]
    // Row 8
    [InlineData("20.001", 7, 30, 9,  0)]
    [InlineData("22.0",   7, 30, 9,  0)]
    [InlineData("24.0",   7, 30, 9,  0)]
    public void WeightInsideSupportedRange_ReturnsExpectedRange(
        string pounds, int minH, int minM, int maxH, int maxM)
    {
        var d = new TurkeyMeal().Calculate(Weight.FromPounds(decimal.Parse(pounds)));
        d.Minimum.Should().Be(new TimeSpan(minH, minM, 0));
        d.Maximum.Should().Be(new TimeSpan(maxH, maxM, 0));
    }

    [Theory]
    [InlineData("5.9999")]
    [InlineData("24.0001")]
    [InlineData("0")]
    [InlineData("100")]
    public void WeightOutsideRange_Throws(string pounds)
    {
        var act = () => new TurkeyMeal().Calculate(Weight.FromPounds(decimal.Parse(pounds)));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

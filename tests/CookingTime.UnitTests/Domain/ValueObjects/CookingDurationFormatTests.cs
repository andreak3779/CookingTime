// filepath: tests/CookingTime.UnitTests/Domain/ValueObjects/CookingDurationFormatTests.cs
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.ValueObjects;

public sealed class CookingDurationFormatTests
{
    [Fact]
    public void Single_WholeHours_FormatsAsHours()
    {
        var d = CookingDuration.Single(new TimeSpan(2, 0, 0));
        d.Format().Should().Be("Cooking time will be 2hrs");
    }

    [Fact]
    public void Single_HoursAndMinutes_FormatsWithMinutes()
    {
        var d = CookingDuration.Single(new TimeSpan(2, 30, 0));
        d.Format().Should().Be("Cooking time will be 2hrs and 30");
    }

    [Fact]
    public void Range_FormatsAsLegacyBetweenPhrase()
    {
        var d = new CookingDuration(new TimeSpan(1, 15, 0), new TimeSpan(2, 0, 0));
        d.Format().Should().Be("Cooking time will be between of 1 hrs. and 15 mins. to 2 hrs. and 0 mins.");
    }

    [Fact]
    public void Constructor_NegativeMinimum_Throws()
    {
        var act = () => new CookingDuration(TimeSpan.FromMinutes(-1), TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeMaximum_Throws()
    {
        var act = () => new CookingDuration(TimeSpan.Zero, TimeSpan.FromMinutes(-1));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

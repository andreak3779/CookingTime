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

    [Fact]
    public void Constructor_MaximumBelowMinimum_Throws()
    {
        // Phase 14: an inverted range is not a duration. The legacy code
        // allowed it and Format() rendered "between of 5 hrs. and 0 mins. to
        // 1 hrs. and 0 mins." to the user.
        var act = () => new CookingDuration(TimeSpan.FromHours(5), TimeSpan.FromHours(1));
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Maximum*must be greater than or equal to minimum*");
    }

    [Fact]
    public void Constructor_MaximumEqualToMinimum_IsAllowed()
    {
        var d = new CookingDuration(TimeSpan.FromHours(2), TimeSpan.FromHours(2));
        d.Minimum.Should().Be(d.Maximum);
    }
}

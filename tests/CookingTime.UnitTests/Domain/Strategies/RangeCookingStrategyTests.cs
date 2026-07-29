// filepath: tests/CookingTime.UnitTests/Domain/Strategies/RangeCookingStrategyTests.cs
using CookingTime.Domain.Strategies;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Strategies;

public sealed class RangeCookingStrategyTests
{
    [Fact]
    public void SingleValue_ProducesMinEqualsMax()
    {
        var s = new RangeCookingStrategy(40);
        var d = s.Calculate(Weight.FromPounds(5m));
        d.Minimum.Should().Be(TimeSpan.FromMinutes(200));
        d.Maximum.Should().Be(TimeSpan.FromMinutes(200));
    }

    [Fact]
    public void Range_ProducesMinAndMax()
    {
        var s = new RangeCookingStrategy(40, 45);
        var d = s.Calculate(Weight.FromPounds(5m));
        d.Minimum.Should().Be(TimeSpan.FromMinutes(200));
        d.Maximum.Should().Be(TimeSpan.FromMinutes(225));
    }

    [Fact]
    public void FractionalWeight_RoundsToNearestMinute()
    {
        var s = new RangeCookingStrategy(30);
        var d = s.Calculate(Weight.FromPounds(3.5m));
        d.Minimum.Should().Be(TimeSpan.FromMinutes(105));
        d.Maximum.Should().Be(TimeSpan.FromMinutes(105));
    }

    [Fact]
    public void Constructor_ZeroMin_Throws()
    {
        var act = () => new RangeCookingStrategy(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeMax_Throws()
    {
        var act = () => new RangeCookingStrategy(40, -5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

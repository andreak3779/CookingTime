// filepath: tests/CookingTime.UnitTests/Domain/Strategies/TableLookupStrategyTests.cs
using CookingTime.Domain.Strategies;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Strategies;

public sealed class TableLookupStrategyTests
{
    private static TableLookupStrategy BuildStrategy() => new(new[]
    {
        (1.5M,  2.5M,  new TimeSpan(1, 15, 0), new TimeSpan(2,  0, 0)),
        (2.5M,  3.5M,  new TimeSpan(2,  0, 0), new TimeSpan(3,  0, 0)),
        (3.5M,  4.75M, new TimeSpan(3,  0, 0), new TimeSpan(3, 30, 0)),
        (4.75M, 6.0M,  new TimeSpan(3, 30, 0), new TimeSpan(4,  0, 0)),
    });

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(2.5)]
    public void WeightInsideFirstRow_ReturnsFirstRange(double pounds)
    {
        var d = BuildStrategy().Calculate(Weight.FromPounds((decimal)pounds));
        d.Minimum.Should().Be(new TimeSpan(1, 15, 0));
        d.Maximum.Should().Be(new TimeSpan(2, 0, 0));
    }

    [Theory]
    [InlineData(2.5001)]
    [InlineData(3.0)]
    [InlineData(3.5)]
    public void WeightInsideSecondRow_ReturnsSecondRange(double pounds)
    {
        var d = BuildStrategy().Calculate(Weight.FromPounds((decimal)pounds));
        d.Minimum.Should().Be(new TimeSpan(2, 0, 0));
        d.Maximum.Should().Be(new TimeSpan(3, 0, 0));
    }

    [Theory]
    [InlineData("1.4999")]
    [InlineData("6.0001")]
    [InlineData("0")]
    [InlineData("100")]
    public void WeightOutsideAnyRow_Throws(string pounds)
    {
        var act = () => BuildStrategy().Calculate(Weight.FromPounds(decimal.Parse(pounds)));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_EmptyRows_Throws()
    {
        var act = () => new TableLookupStrategy(Array.Empty<(decimal, decimal, TimeSpan, TimeSpan)>());
        act.Should().Throw<ArgumentException>();
    }
}

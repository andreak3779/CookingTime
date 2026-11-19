// filepath: tests/CookingTime.UnitTests/Domain/ValueObjects/WeightTests.cs
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.ValueObjects;

public sealed class WeightTests
{
    [Fact]
    public void FromPounds_StoresCanonicalValue()
    {
        var w = Weight.FromPounds(5m);
        w.Pounds.Should().Be(5m);
    }

    [Fact]
    public void FromKilograms_ConvertsToPounds()
    {
        // 5 kg × 2.20462262 = 11.0231131 lbs
        var w = Weight.FromKilograms(5m);
        w.Pounds.Should().BeApproximately(11.0231131M, 0.0001M);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FromPounds_NonPositive_Throws(decimal pounds)
    {
        var act = () => Weight.FromPounds(pounds);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FromKilograms_NonPositive_Throws(decimal kilograms)
    {
        var act = () => Weight.FromKilograms(kilograms);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToKilograms_InvertsFromKilograms()
    {
        var w = Weight.FromKilograms(5m);
        w.ToKilograms().Should().BeApproximately(5m, 0.0001M);
    }

    [Fact]
    public void Records_AreEqualByPoundsValue()
    {
        var a = Weight.FromPounds(3.5M);
        var b = Weight.FromPounds(3.5M);
        a.Should().Be(b);
    }

    [Fact]
    public void ComparisonOperators_OrderByPounds()
    {
        // Phase 14: ordering lets a meal range-check a weight directly
        // ("weight < MinimumWeight") instead of reaching into .Pounds.
        var light = Weight.FromPounds(1.5M);
        var heavy = Weight.FromPounds(6M);
        var sameAsLight = Weight.FromPounds(1.5M);

        (light < heavy).Should().BeTrue();
        (heavy > light).Should().BeTrue();
        (light <= heavy).Should().BeTrue();
        (heavy >= light).Should().BeTrue();
        (light > heavy).Should().BeFalse();
        (light < sameAsLight).Should().BeFalse();
        (light <= sameAsLight).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_OrdersByPounds()
    {
        var light = Weight.FromPounds(1.5M);
        var heavy = Weight.FromPounds(6M);

        light.CompareTo(heavy).Should().BeNegative();
        heavy.CompareTo(light).Should().BePositive();
        light.CompareTo(Weight.FromPounds(1.5M)).Should().Be(0);
    }
}

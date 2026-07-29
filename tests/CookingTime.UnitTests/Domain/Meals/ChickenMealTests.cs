// filepath: tests/CookingTime.UnitTests/Domain/Meals/ChickenMealTests.cs
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Meals;

public sealed class ChickenMealTests
{
    [Fact]
    public void Name_IsChicken()
    {
        new ChickenMeal().Name.Should().Be("Chicken");
    }

    [Fact]
    public void Instructions_AreNotEmpty()
    {
        new ChickenMeal().Instructions.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MinimumWeight_IsOnePointFivePounds()
    {
        new ChickenMeal().MinimumWeight.Pounds.Should().Be(1.5M);
    }

    [Fact]
    public void MaximumWeight_IsSixPounds()
    {
        new ChickenMeal().MaximumWeight.Pounds.Should().Be(6.0M);
    }

    [Fact]
    public void TwoPounds_YieldsRangeOf75To120Minutes()
    {
        var d = new ChickenMeal().Calculate(Weight.FromPounds(2m));
        d.Minimum.Should().Be(new TimeSpan(1, 15, 0));
        d.Maximum.Should().Be(new TimeSpan(2, 0, 0));
    }

    [Theory]
    [InlineData("1.4999")]
    [InlineData("6.0001")]
    public void OutOfRange_Throws(string pounds)
    {
        var act = () => new ChickenMeal().Calculate(Weight.FromPounds(decimal.Parse(pounds)));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

// filepath: tests/CookingTime.UnitTests/Application/CookingTimeCalculatorTests.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Common;
using CookingTime.Application.Services;
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;
using Moq;

namespace CookingTime.UnitTests.Application;

public sealed class CookingTimeCalculatorTests
{
    private static Mock<IMealFactory> FactoryWith(params IMeal[] meals)
    {
        var mock = new Mock<IMealFactory>();
        mock.Setup(f => f.CreateAll()).Returns(meals);
        return mock;
    }

    private sealed class TestMeal : IMeal
    {
        public string Name { get; }
        public string Instructions { get; }
        public MealKind Kind { get; }
        public Weight MinimumWeight { get; }
        public Weight MaximumWeight { get; }
        private readonly Func<Weight, CookingDuration> _calc;
        public TestMeal(string name, string instructions, MealKind kind, Weight min, Weight max,
                        Func<Weight, CookingDuration>? calc = null)
        {
            Name = name;
            Instructions = instructions;
            Kind = kind;
            MinimumWeight = min;
            MaximumWeight = max;
            _calc = calc ?? (_ => new CookingDuration(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(40)));
        }
        public CookingDuration Calculate(Weight weight) => _calc(weight);
    }

    [Fact]
    public async Task CalculateAsync_ChickenTwoPounds_ReturnsFormattedRange()
    {
        var chicken = new TestMeal("Chicken", "Preheat oven...", MealKind.Chicken,
                                   Weight.FromPounds(1.5M), Weight.FromPounds(6M),
                                   _ => new CookingDuration(new TimeSpan(1, 15, 0), new TimeSpan(2, 0, 0)));
        var factory = FactoryWith(chicken);
        var sut = new CookingTimeCalculator(factory.Object);

        var result = await sut.CalculateAsync("Chicken", 2M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeTrue();
        result.FormattedDuration.Should()
            .Be("Cooking time will be between of 1 hrs. and 15 mins. to 2 hrs. and 0 mins.");
        result.Instructions.Should().Contain("Preheat");
    }

    [Fact]
    public async Task CalculateAsync_KilogramsInput_PassesConvertedWeightToMeal()
    {
        Weight? captured = null;
        var meal = new TestMeal("Chicken", "x", MealKind.Chicken,
                                Weight.FromPounds(1.5M), Weight.FromPounds(6M),
                                w => { captured = w; return new CookingDuration(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(40)); });
        var factory = FactoryWith(meal);
        var sut = new CookingTimeCalculator(factory.Object);

        await sut.CalculateAsync("Chicken", 2M, WeightUnit.Kilograms);

        // 2 kg = 4.40924524 lbs
        captured.Should().NotBeNull();
        captured!.Value.Pounds.Should().BeApproximately(4.40924524M, 0.0001M);
    }

    [Fact]
    public async Task CalculateAsync_UnknownMeal_ReturnsValidationError()
    {
        var sut = new CookingTimeCalculator(FactoryWith().Object);

        var result = await sut.CalculateAsync("Mystery", 2M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainMatch("*Unknown meal*");
    }

    [Fact]
    public async Task CalculateAsync_WeightOutOfRange_ReturnsValidationError()
    {
        var chicken = new TestMeal("Chicken", "x", MealKind.Chicken,
                                   Weight.FromPounds(1.5M), Weight.FromPounds(6M),
                                   _ => throw new ArgumentOutOfRangeException("weight"));
        var sut = new CookingTimeCalculator(FactoryWith(chicken).Object);

        var result = await sut.CalculateAsync("Chicken", 25M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_BlankMealName_ReturnsValidationError()
    {
        var sut = new CookingTimeCalculator(FactoryWith().Object);

        var result = await sut.CalculateAsync("", 2M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Please select a meal.");
    }

    [Fact]
    public async Task CalculateAsync_NonPositiveWeight_ReturnsValidationError()
    {
        var sut = new CookingTimeCalculator(FactoryWith().Object);

        var result = await sut.CalculateAsync("Chicken", 0M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain("Please enter a weight greater than zero.");
    }

    [Fact]
    public async Task CalculateAsync_NegativeWeight_ReturnsValidationError()
    {
        var sut = new CookingTimeCalculator(FactoryWith().Object);

        var result = await sut.CalculateAsync("Chicken", -2M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateAsync_CancelledToken_Throws()
    {
        var sut = new CookingTimeCalculator(FactoryWith().Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.CalculateAsync("Chicken", 2M, WeightUnit.Pounds, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CalculateAsync_VerifiesCreateAllCalledExactlyOnce()
    {
        var factory = new Mock<IMealFactory>();
        factory.Setup(f => f.CreateAll())
               .Returns(new IMeal[]
               {
                   new TestMeal("Chicken", "x", MealKind.Chicken,
                                Weight.FromPounds(1), Weight.FromPounds(10))
               });

        var sut = new CookingTimeCalculator(factory.Object);
        await sut.CalculateAsync("Chicken", 2M, WeightUnit.Pounds);

        factory.Verify(f => f.CreateAll(), Times.Once);
    }

    [Fact]
    public void CookingResult_Success_HasNoErrors()
    {
        var r = CookingResult.Success("x", "y");
        r.IsSuccess.Should().BeTrue();
        r.FormattedDuration.Should().Be("x");
        r.Instructions.Should().Be("y");
        r.ValidationErrors.Should().BeEmpty();
    }

    [Fact]
    public void CookingResult_Failure_HasErrorsAndEmptyPayload()
    {
        var r = CookingResult.Failure("err1", "err2");
        r.IsSuccess.Should().BeFalse();
        r.FormattedDuration.Should().BeEmpty();
        r.Instructions.Should().BeEmpty();
        r.ValidationErrors.Should().BeEquivalentTo(new[] { "err1", "err2" });
    }
}

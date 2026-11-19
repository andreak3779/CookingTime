// filepath: tests/CookingTime.UnitTests/Application/CookingTimeCalculatorTests.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Common;
using CookingTime.Application.Services;
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;
using Moq;

namespace CookingTime.UnitTests.Application;

public sealed class CookingTimeCalculatorTests
{
    private static Mock<IMealFactory> FactoryWith(params IMeal[] meals)
    {
        var mock = new Mock<IMealFactory>();
        mock.Setup(f => f.CreateAll()).Returns(meals);
        // Phase 17: the calculator asks the factory for a meal by name
        // instead of rebuilding the catalog. Mirror the real factory's
        // case-insensitive name index so the mock behaves like the real thing.
        mock.Setup(f => f.TryGetByName(It.IsAny<string>(), out It.Ref<IMeal?>.IsAny))
            .Returns((string name, out IMeal? meal) =>
            {
                meal = meals.FirstOrDefault(m =>
                    string.Equals(m.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase));
                return meal is not null;
            });
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
    public async Task CalculateAsync_DoesNotRebuildTheCatalog_PerCalculation()
    {
        // Phase 17: the calculator used to call CreateAll() - which
        // re-invoked every factory delegate - on every single calculation.
        // It now asks the factory for the meal by name, so the catalog is
        // not rebuilt per calculation.
        var factory = new Mock<IMealFactory>();
        var chicken = new TestMeal("Chicken", "x", MealKind.Chicken,
            Weight.FromPounds(1), Weight.FromPounds(10));
        factory.Setup(f => f.CreateAll()).Returns(new IMeal[] { chicken });
        factory.Setup(f => f.TryGetByName(It.IsAny<string>(), out It.Ref<IMeal?>.IsAny))
            .Returns((string name, out IMeal? meal) =>
            {
                meal = chicken;
                return true;
            });

        var sut = new CookingTimeCalculator(factory.Object);
        await sut.CalculateAsync("Chicken", 2M, WeightUnit.Pounds);
        await sut.CalculateAsync("Chicken", 3M, WeightUnit.Pounds);

        factory.Verify(f => f.CreateAll(), Times.Never);
        factory.Verify(f => f.TryGetByName("Chicken", out It.Ref<IMeal?>.IsAny), Times.Exactly(2));
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

    // Phase 7: the three branches below push the UnitTests line coverage
    // above the 75% production-code threshold the CI gate enforces. They
    // target the WeightUnit switch's _ => throw default, the catch block
    // around Weight.FromKilograms when the kilograms input is out of range,
    // and the catch block around meal.Calculate when the meal rejects the
    // converted weight.

    [Fact]
    public async Task CalculateAsync_InvalidWeightUnitEnum_ReturnsValidationError()
    {
        var meal = new TestMeal("Chicken", "x", MealKind.Chicken,
            Weight.FromPounds(1), Weight.FromPounds(20));
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Chicken", 2M, (WeightUnit)999);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle()
            .Which.Should().Contain("Unknown weight unit");
    }

    [Fact]
    public async Task CalculateAsync_NegativeKilograms_ReturnsValidationError()
    {
        var meal = new TestMeal("Chicken", "x", MealKind.Chicken,
            Weight.FromPounds(1), Weight.FromPounds(20));
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Chicken", -1M, WeightUnit.Kilograms);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_MealRejectsWeight_ReturnsValidationError()
    {
        // The meal's domain throws ArgumentOutOfRangeException for weights
        // outside its range. The calculator catches and surfaces it.
        var meal = new TestMeal("Chicken", "x", MealKind.Chicken,
            Weight.FromPounds(5), Weight.FromPounds(10),
            calc: _ => throw new ArgumentOutOfRangeException(nameof(Weight)));
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Chicken", 1M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle()
            .Which.Should().Contain("outside the supported range");
    }

    [Fact]
    public async Task CalculateAsync_RealRangeMeal_AboveMaximumWeight_ReturnsValidationError()
    {
        // Phase 14 regression, end-to-end through the real domain type.
        // Before the fix RangeMeal accepted any weight, so a 500 lb pork
        // roast returned IsSuccess=true with an inverted duration
        // ("between of 21 hrs. and 20 mins. to 15 hrs. and 0 mins.").
        // ChickenMeal already rejected the same input, so the two meal
        // kinds disagreed about what a valid weight is.
        var meal = new RangeMeal("Pork Roast", "Cook it.", 40, 45, 1M, 24M);
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Pork Roast", 500M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.FormattedDuration.Should().BeEmpty();
        result.ValidationErrors.Should().ContainSingle()
            .Which.Should().Contain("outside the supported range for Pork Roast");
    }

    [Fact]
    public async Task CalculateAsync_RealRangeMeal_BelowMinimumWeight_ReturnsValidationError()
    {
        // Phase 14 regression: a 0.5 lb roast produced a 20-minute cook time
        // and was reported as a success.
        var meal = new RangeMeal("Pork Roast", "Cook it.", 40, 45, 1M, 24M);
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Pork Roast", 0.5M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle()
            .Which.Should().Contain("outside the supported range for Pork Roast");
    }

    [Fact]
    public async Task CalculateAsync_RealRangeMeal_WithinRange_Succeeds()
    {
        // The fix must not break the normal path.
        var meal = new RangeMeal("Pork Roast", "Cook it.", 40, 45, 1M, 24M);
        var sut = new CookingTimeCalculator(FactoryWith(meal).Object);

        var result = await sut.CalculateAsync("Pork Roast", 5M, WeightUnit.Pounds);

        result.IsSuccess.Should().BeTrue();
        result.FormattedDuration.Should().Contain("3 hrs. and 20 mins.");
    }
}

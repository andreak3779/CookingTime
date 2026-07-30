// filepath: tests/CookingTime.UnitTests/Domain/Meals/MealTypeRegistryTests.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Meals;

public sealed class MealTypeRegistryTests
{
    [Fact]
    public void Register_ThenTryCreate_ReturnsRegisteredMeal()
    {
        var registry = new MealTypeRegistry();
        var expected = new ChickenMeal();
        registry.Register(MealKind.Chicken, () => expected);

        var ok = registry.TryCreate(MealKind.Chicken, out var actual);

        ok.Should().BeTrue();
        actual.Should().BeSameAs(expected);
    }

    [Fact]
    public void TryCreate_UnknownKind_ReturnsFalseAndNullMeal()
    {
        var registry = new MealTypeRegistry();
        var ok = registry.TryCreate(MealKind.Turkey, out var meal);
        ok.Should().BeFalse();
        meal.Should().BeNull();
    }

    [Fact]
    public void TryCreate_FactoryReturnsNull_Throws()
    {
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => null!);

        var act = () => registry.TryCreate(MealKind.Chicken, out _);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisteredKinds_ReflectsRegisteredEntries()
    {
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => new ChickenMeal());
        registry.Register(MealKind.Turkey, () => new TurkeyMeal());

        registry.RegisteredKinds.Should().BeEquivalentTo(new[] { MealKind.Chicken, MealKind.Turkey });
    }

    [Fact]
    public void Register_NullFactory_Throws()
    {
        var registry = new MealTypeRegistry();
        var act = () => registry.Register(MealKind.Chicken, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateAll_ReturnsEveryRegisteredMeal()
    {
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => new ChickenMeal());
        registry.Register(MealKind.Turkey, () => new TurkeyMeal());

        var meals = registry.CreateAll();

        meals.Should().HaveCount(2);
        meals.Select(m => m.Kind).Should().BeEquivalentTo(new[] { MealKind.Chicken, MealKind.Turkey });
    }

    [Fact]
    public void CreateAll_FactoryReturnsNull_Throws()
    {
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => null!);

        var act = () => registry.CreateAll();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateAll_EmptyRegistry_ReturnsEmptyList()
    {
        var registry = new MealTypeRegistry();
        registry.CreateAll().Should().BeEmpty();
    }
}

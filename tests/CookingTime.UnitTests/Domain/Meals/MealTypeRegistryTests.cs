// filepath: tests/CookingTime.UnitTests/Domain/Meals/MealTypeRegistryTests.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Meals;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.UnitTests.Domain.Meals;

public sealed class MealTypeRegistryTests
{
    [Fact]
    public void CreateAll_UnknownKind_IsSimplyAbsent()
    {
        // Phase 17 replaced TryCreate with CreateAll + the factory's own name
        // index. A kind that was never registered simply does not appear.
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => new ChickenMeal());

        var meals = registry.CreateAll();

        meals.Should().HaveCount(1);
        meals.Select(m => m.Kind).Should().NotContain(MealKind.Turkey);
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

    [Fact]
    public void Register_MultipleFactoriesForSameKind_CreateAllReturnsAllOfThem()
    {
        // Regression test: MealKind.Range is a category shared by many distinct
        // configured meals (roasts, hams, etc). Registering several factories
        // under the same kind must not overwrite earlier registrations.
        var registry = new MealTypeRegistry();
        var first = new RangeMeal("Pork Roast", "x", 40, 45, 1, 24);
        var second = new RangeMeal("Beef Roast", "y", 18, 20, 1, 24);
        var third = new RangeMeal("Smoked Ham", "z", 15, 18, 1, 24);

        registry.Register(MealKind.Range, () => first);
        registry.Register(MealKind.Range, () => second);
        registry.Register(MealKind.Range, () => third);

        var meals = registry.CreateAll();

        meals.Should().HaveCount(3);
        meals.Select(m => m.Name).Should().BeEquivalentTo(new[] { "Pork Roast", "Beef Roast", "Smoked Ham" });
    }

}

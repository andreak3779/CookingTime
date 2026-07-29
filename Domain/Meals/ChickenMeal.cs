// filepath: Domain/Meals/ChickenMeal.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Strategies;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Meals;

/// <summary>
/// Chicken. Time ranges preserved verbatim from the legacy
/// Backup/ComCookingTime.cs MealChicken table.
/// </summary>
public sealed class ChickenMeal : IMeal
{
    public string Name => "Chicken";
    public string Instructions =>
        "Preheat oven to 325F degrees. The chicken will be ready when " +
        "its internal temperature is at 180F degrees.";
    public MealKind Kind => MealKind.Chicken;

    public Weight MinimumWeight => Weight.FromPounds(1.5M);
    public Weight MaximumWeight => Weight.FromPounds(6.0M);

    private static readonly TableLookupStrategy Strategy = new(new[]
    {
        (1.5M,   2.5M,    new TimeSpan(1, 15, 0), new TimeSpan(2,  0, 0)),
        (2.5M,   3.5M,    new TimeSpan(2,  0, 0), new TimeSpan(3,  0, 0)),
        (3.5M,   4.75M,   new TimeSpan(3,  0, 0), new TimeSpan(3, 30, 0)),
        (4.75M,  6.0M,    new TimeSpan(3, 30, 0), new TimeSpan(4,  0, 0)),
    });

    public CookingDuration Calculate(Weight weight) => Strategy.Calculate(weight);
}

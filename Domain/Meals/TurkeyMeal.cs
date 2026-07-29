// filepath: Domain/Meals/TurkeyMeal.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Strategies;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Meals;

/// <summary>
/// Turkey. Time ranges preserved verbatim from the legacy
/// Backup/ComCookingTime.cs MealTurkey table.
/// </summary>
public sealed class TurkeyMeal : IMeal
{
    public string Name => "Turkey";
    public string Instructions =>
        "Preheat oven to 325F degrees. Cook until the internal temperature " +
        "in the thigh reaches 165F degrees.";
    public MealKind Kind => MealKind.Turkey;

    public Weight MinimumWeight => Weight.FromPounds(6.0M);
    public Weight MaximumWeight => Weight.FromPounds(24.0M);

    private static readonly TableLookupStrategy Strategy = new(new[]
    {
        (6.0M,   8.0M,   new TimeSpan(3, 45, 0), new TimeSpan(4,  0, 0)),
        (8.0M,  10.0M,   new TimeSpan(4,  0, 0), new TimeSpan(4, 30, 0)),
        (10.0M, 12.0M,   new TimeSpan(4, 30, 0), new TimeSpan(5,  0, 0)),
        (12.0M, 14.0M,   new TimeSpan(5,  0, 0), new TimeSpan(5, 15, 0)),
        (14.0M, 16.0M,   new TimeSpan(5, 15, 0), new TimeSpan(6,  0, 0)),
        (16.0M, 18.0M,   new TimeSpan(6,  0, 0), new TimeSpan(6, 30, 0)),
        (18.0M, 20.0M,   new TimeSpan(6, 30, 0), new TimeSpan(7, 30, 0)),
        (20.0M, 24.0M,   new TimeSpan(7, 30, 0), new TimeSpan(9,  0, 0)),
    });

    public CookingDuration Calculate(Weight weight) => Strategy.Calculate(weight);
}

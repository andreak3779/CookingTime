// filepath: Domain/Meals/RangeMeal.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Strategies;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Meals;

/// <summary>
/// A meal whose cooking time scales linearly with weight (range formula).
/// Used for the beef, pork, and ham entries in the legacy catalog.
/// </summary>
public sealed class RangeMeal : IMeal
{
    public string Name { get; }
    public string Instructions { get; }
    public MealKind Kind => MealKind.Range;
    public Weight MinimumWeight { get; }
    public Weight MaximumWeight { get; }

    private readonly RangeCookingStrategy _strategy;

    public RangeMeal(
        string name,
        string instructions,
        int minMinutesPerPound,
        int maxMinutesPerPound,
        decimal minPounds,
        decimal maxPounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(instructions);
        if (minPounds <= 0M)
            throw new ArgumentOutOfRangeException(nameof(minPounds), minPounds, "Must be positive.");
        if (maxPounds <= 0M || maxPounds < minPounds)
            throw new ArgumentOutOfRangeException(nameof(maxPounds), maxPounds, "Must be positive and ≥ minPounds.");

        Name = name;
        Instructions = instructions;
        MinimumWeight = Weight.FromPounds(minPounds);
        MaximumWeight = Weight.FromPounds(maxPounds);
        _strategy = new RangeCookingStrategy(minMinutesPerPound, maxMinutesPerPound);
    }

    public CookingDuration Calculate(Weight weight)
    {
        // Phase 14: the meal owns its weight range, so the meal - not the
        // strategy - enforces it. Without this guard every configured meal
        // accepted any weight: a 500 lb pork roast produced "between of
        // 21 hrs. and 20 mins. to 15 hrs. and 0 mins." and the application
        // layer reported it as a SUCCESS. ChickenMeal already behaved
        // correctly (TableLookupStrategy throws outside its table), so the
        // two meal kinds disagreed. Throwing here lets
        // CookingTimeCalculator surface the same "outside the supported
        // range" message for both.
        if (weight < MinimumWeight || weight > MaximumWeight)
            throw new ArgumentOutOfRangeException(
                nameof(weight), weight.Pounds,
                $"Weight is outside the supported range for {Name} " +
                $"({MinimumWeight.Pounds:0.##}-{MaximumWeight.Pounds:0.##} lbs).");

        return _strategy.Calculate(weight);
    }
}

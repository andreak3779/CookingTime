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

    public CookingDuration Calculate(Weight weight) => _strategy.Calculate(weight);
}

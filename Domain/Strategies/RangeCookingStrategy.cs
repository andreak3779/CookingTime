// filepath: Domain/Strategies/RangeCookingStrategy.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Strategies;

/// <summary>
/// Linear cooking time: total = weight * timePerLb. When only Min is given,
/// Min == Max (single-value duration). When both are given, produces a
/// [min, max] range.
/// </summary>
public sealed class RangeCookingStrategy : ICookingStrategy
{
    public int MinMinutesPerPound { get; }
    public int MaxMinutesPerPound { get; }

    public RangeCookingStrategy(int minMinutesPerPound, int? maxMinutesPerPound = null)
    {
        if (minMinutesPerPound <= 0)
            throw new ArgumentOutOfRangeException(nameof(minMinutesPerPound), minMinutesPerPound, "Must be positive.");
        if (maxMinutesPerPound is not null && maxMinutesPerPound.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMinutesPerPound), maxMinutesPerPound.Value, "Must be positive.");
        // Phase 14: an inverted per-pound range would produce a duration
        // whose minimum exceeds its maximum. CookingDuration now rejects
        // that, but failing here names the actual cause - a bad catalog
        // entry - instead of surfacing it later as an arithmetic error.
        var max = maxMinutesPerPound ?? minMinutesPerPound;
        if (max < minMinutesPerPound)
            throw new ArgumentOutOfRangeException(
                nameof(maxMinutesPerPound), max,
                $"Must be greater than or equal to minMinutesPerPound ({minMinutesPerPound}).");
        MinMinutesPerPound = minMinutesPerPound;
        MaxMinutesPerPound = max;
    }

    public CookingDuration Calculate(Weight weight)
    {
        var lbs = weight.Pounds;
        var minMinutes = (int)Math.Round(lbs * MinMinutesPerPound);
        var maxMinutes = (int)Math.Round(lbs * MaxMinutesPerPound);
        return new CookingDuration(TimeSpan.FromMinutes(minMinutes), TimeSpan.FromMinutes(maxMinutes));
    }
}

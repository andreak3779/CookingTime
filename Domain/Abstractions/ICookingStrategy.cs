// filepath: Domain/Abstractions/ICookingStrategy.cs
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Abstractions;

/// <summary>
/// Pluggable cooking-time algorithm. Throws <see cref="ArgumentOutOfRangeException"/>
/// when the weight is outside the strategy's known range; the application
/// layer catches and surfaces a validation error to the UI.
/// </summary>
public interface ICookingStrategy
{
    CookingDuration Calculate(Weight weight);
}

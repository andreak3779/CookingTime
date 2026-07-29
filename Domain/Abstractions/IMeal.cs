// filepath: Domain/Abstractions/IMeal.cs
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Domain.Abstractions;

/// <summary>
/// A meal that can be cooked, exposed to the application/UI layers.
/// </summary>
public interface IMeal
{
    string Name { get; }
    string Instructions { get; }
    MealKind Kind { get; }
    Weight MinimumWeight { get; }
    Weight MaximumWeight { get; }
    CookingDuration Calculate(Weight weight);
}

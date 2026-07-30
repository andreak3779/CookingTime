// filepath: Application/Abstractions/ICookingTimeCalculator.cs
using CookingTime.Application.Common;

namespace CookingTime.Application.Abstractions;

public enum WeightUnit
{
    Pounds = 1,
    Kilograms = 2,
}

/// <summary>
/// Public surface for the cooking-time calculator. The UI layer (Phase 4)
/// depends on this abstraction; the concrete implementation
/// (Application/Services/CookingTimeCalculator.cs) is wired by DI.
/// </summary>
public interface ICookingTimeCalculator
{
    Task<CookingResult> CalculateAsync(
        string mealName,
        decimal enteredWeight,
        WeightUnit unit,
        CancellationToken cancellationToken = default);
}

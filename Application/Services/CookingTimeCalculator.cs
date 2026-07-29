// filepath: Application/Services/CookingTimeCalculator.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Common;
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.Application.Services;

/// <summary>
/// Wraps the domain behind a UI-friendly API. Catches domain
/// <see cref="ArgumentOutOfRangeException"/>s and surfaces them as
/// <see cref="CookingResult.ValidationErrors"/>; never throws for
/// expected validation failures.
/// </summary>
public sealed class CookingTimeCalculator : ICookingTimeCalculator
{
    private readonly IMealFactory _mealFactory;

    public CookingTimeCalculator(IMealFactory mealFactory)
    {
        ArgumentNullException.ThrowIfNull(mealFactory);
        _mealFactory = mealFactory;
    }

    public Task<CookingResult> CalculateAsync(
        string mealName,
        decimal enteredWeight,
        WeightUnit unit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(mealName))
            errors.Add("Please select a meal.");
        if (enteredWeight <= 0M)
            errors.Add("Please enter a weight greater than zero.");
        if (errors.Count > 0)
            return Task.FromResult(CookingResult.Failure(errors.ToArray()));

        var meal = _mealFactory.CreateAll()
            .FirstOrDefault(m => string.Equals(m.Name, mealName, StringComparison.OrdinalIgnoreCase));
        if (meal is null)
            return Task.FromResult(CookingResult.Failure($"Unknown meal '{mealName}'."));

        Weight weight;
        try
        {
            weight = unit switch
            {
                WeightUnit.Pounds => Weight.FromPounds(enteredWeight),
                WeightUnit.Kilograms => Weight.FromKilograms(enteredWeight),
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown weight unit."),
            };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Task.FromResult(CookingResult.Failure(ex.Message));
        }

        CookingDuration duration;
        try
        {
            duration = meal.Calculate(weight);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Task.FromResult(CookingResult.Failure(
                $"Weight {weight.Pounds:0.##} lbs is outside the supported range for {meal.Name}."));
        }

        return Task.FromResult(CookingResult.Success(duration.Format(), meal.Instructions));
    }
}

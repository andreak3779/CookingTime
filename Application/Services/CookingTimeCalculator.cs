// filepath: Application/Services/CookingTimeCalculator.cs
using CookingTime.Application.Abstractions;
using CookingTime.Application.Common;
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<CookingTimeCalculator> _logger;

    public CookingTimeCalculator(
        IMealFactory mealFactory,
        ILogger<CookingTimeCalculator>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(mealFactory);
        _mealFactory = mealFactory;
        // Optional so the existing unit tests can construct the calculator
        // without a logging pipeline; NullLogger writes nowhere.
        _logger = logger ?? NullLogger<CookingTimeCalculator>.Instance;
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

        // Phase 17: ask the factory directly instead of rebuilding the whole
        // catalog on every calculation.
        if (!_mealFactory.TryGetByName(mealName, out var meal) || meal is null)
        {
            // Phase 17: this is the one failure a user can trigger that is
            // not a plain validation error. Log it so a mis-configured
            // catalog is diagnosable rather than invisible.
            _logger.LogWarning(
                "Calculation requested for unknown meal {MealName}.", mealName);
            return Task.FromResult(CookingResult.Failure($"Unknown meal '{mealName}'."));
        }

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
            // A weight the meal does not support. Expected user input, but
            // worth a Debug line: a systematic spike means the UI's allowed
            // range and the meal's range have drifted apart.
            _logger.LogDebug(
                "Weight {Weight} lbs is outside the supported range for {MealName}.",
                weight.Pounds, meal.Name);
            return Task.FromResult(CookingResult.Failure(
                $"Weight {weight.Pounds:0.##} lbs is outside the supported range for {meal.Name}."));
        }

        return Task.FromResult(CookingResult.Success(duration.Format(), meal.Instructions));
    }
}

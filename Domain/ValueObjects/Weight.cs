// filepath: Domain/ValueObjects/Weight.cs
namespace CookingTime.Domain.ValueObjects;

/// <summary>
/// Canonical cooking-time weight, expressed in pounds. Construct via the
/// static factories so the kg→lb conversion point is the only place that
/// knows the conversion factor. Immutable by record-struct semantics.
/// </summary>
public readonly record struct Weight
{
    private const decimal KilogramsToPounds = 2.20462262M;

    public decimal Pounds { get; }

    private Weight(decimal pounds) => Pounds = pounds;

    public static Weight FromPounds(decimal pounds)
    {
        if (pounds <= 0M)
            throw new ArgumentOutOfRangeException(nameof(pounds), pounds, "Weight must be positive.");
        return new Weight(pounds);
    }

    public static Weight FromKilograms(decimal kilograms)
    {
        if (kilograms <= 0M)
            throw new ArgumentOutOfRangeException(nameof(kilograms), kilograms, "Weight must be positive.");
        return new Weight(kilograms * KilogramsToPounds);
    }

    public decimal ToKilograms() => Pounds / KilogramsToPounds;
}

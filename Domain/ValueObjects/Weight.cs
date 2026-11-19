// filepath: Domain/ValueObjects/Weight.cs
namespace CookingTime.Domain.ValueObjects;

/// <summary>
/// Canonical cooking-time weight, expressed in pounds. Construct via the
/// static factories so the kg→lb conversion point is the only place that
/// knows the conversion factor. Immutable by record-struct semantics.
///
/// Phase 14: implements <see cref="IComparable{Weight}"/> so weights can be
/// ordered and range-checked (<c>weight &lt; MinimumWeight</c>). A value
/// object whose whole job is to carry a magnitude should support comparing
/// two instances of itself; without it every range guard has to reach into
/// <see cref="Pounds"/>.
/// </summary>
public readonly record struct Weight : IComparable<Weight>
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

    /// <summary>
    /// Orders by <see cref="Pounds"/>. Negative weights cannot be constructed
    /// through the public factories, so no ordering ambiguity arises.
    /// </summary>
    public int CompareTo(Weight other) => Pounds.CompareTo(other.Pounds);

    public static bool operator <(Weight left, Weight right) => left.Pounds < right.Pounds;

    public static bool operator >(Weight left, Weight right) => left.Pounds > right.Pounds;

    public static bool operator <=(Weight left, Weight right) => left.Pounds <= right.Pounds;

    public static bool operator >=(Weight left, Weight right) => left.Pounds >= right.Pounds;
}

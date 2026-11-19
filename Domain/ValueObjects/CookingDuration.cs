// filepath: Domain/ValueObjects/CookingDuration.cs
namespace CookingTime.Domain.ValueObjects;

/// <summary>
/// A cooking duration as a min/max TimeSpan range. Immutable record.
/// Format() reproduces the legacy "Cooking time will be between of X hrs..."
/// text so we can regression-test the legacy output exactly.
/// </summary>
public readonly record struct CookingDuration
{
    public TimeSpan Minimum { get; }
    public TimeSpan Maximum { get; }

    public CookingDuration(TimeSpan minimum, TimeSpan maximum)
    {
        if (minimum < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Must be non-negative.");
        if (maximum < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Must be non-negative.");
        // Phase 14: a range whose maximum is below its minimum is not a
        // duration - it is an inverted one. The legacy code could construct
        // it, and Format() then rendered "between of 21 hrs. and 20 mins. to
        // 15 hrs. and 0 mins." to the user. Reject it at construction so the
        // invalid state is unrepresentable rather than merely unlikely.
        if (maximum < minimum)
            throw new ArgumentException(
                $"Maximum ({maximum}) must be greater than or equal to minimum ({minimum}).",
                nameof(maximum));
        Minimum = minimum;
        Maximum = maximum;
    }

    public static CookingDuration Single(TimeSpan time) => new(time, time);

    public string Format()
    {
        if (Minimum == Maximum)
        {
            return Maximum.Minutes > 0
                ? $"Cooking time will be {Maximum.Hours}hrs and {Maximum.Minutes.ToString()}"
                : $"Cooking time will be {Maximum.Hours}hrs";
        }

        return
            $"Cooking time will be between of {Minimum.Hours} hrs. and {Minimum.Minutes} mins. " +
            $"to {Maximum.Hours} hrs. and {Maximum.Minutes} mins.";
    }
}

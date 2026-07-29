// filepath: Application/Common/CookingResult.cs
namespace CookingTime.Application.Common;

/// <summary>
/// Outcome of a cooking-time calculation. Success carries the formatted
/// duration and the meal's instructions; failure carries one or more
/// human-readable validation messages.
/// </summary>
public sealed class CookingResult
{
    public string FormattedDuration { get; init; } = "";
    public string Instructions { get; init; } = "";
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    public bool IsSuccess => ValidationErrors.Count == 0;

    public static CookingResult Success(string formattedDuration, string instructions) =>
        new() { FormattedDuration = formattedDuration, Instructions = instructions };

    public static CookingResult Failure(params string[] errors) =>
        new() { ValidationErrors = errors };
}

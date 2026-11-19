// filepath: Domain/Meals/MealTypeRegistry.cs
using CookingTime.Domain.Abstractions;

namespace CookingTime.Domain.Meals;

/// <summary>
/// Maps <see cref="MealKind"/> values to factory delegates. New kinds
/// register; they do not edit a switch. That is the OCP rule for meal
/// creation in this codebase. A <see cref="MealKind"/> is a category
/// (e.g. every configured roast is <see cref="MealKind.Range"/>), so
/// multiple factories can be registered under the same kind — each
/// call to <see cref="Register"/> adds to that kind's list rather than
/// replacing it.
/// </summary>
/// <remarks>
/// Phase 17: <c>TryCreate</c> and <c>RegisteredKinds</c> were removed. Both
/// were public API used only by their own tests — never by production code —
/// so they were maintenance surface for a feature nothing called. The
/// registry's job is to produce the catalog, and <see cref="CreateAll"/> does
/// that.
/// </remarks>
public sealed class MealTypeRegistry
{
    private readonly Dictionary<MealKind, List<Func<IMeal>>> _factories = new();

    public void Register(MealKind kind, Func<IMeal> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (!_factories.TryGetValue(kind, out var factoriesForKind))
        {
            factoriesForKind = new List<Func<IMeal>>();
            _factories[kind] = factoriesForKind;
        }
        factoriesForKind.Add(factory);
    }

    /// <summary>
    /// Materialize every registered factory across every kind. Throws
    /// <see cref="InvalidOperationException"/> if any factory returns null.
    /// Order matches the registration order (dictionary/list enumeration order).
    /// </summary>
    public IReadOnlyList<IMeal> CreateAll()
    {
        var result = new List<IMeal>();
        foreach (var (kind, factoriesForKind) in _factories)
        {
            foreach (var factory in factoriesForKind)
            {
                var instance = factory()
                    ?? throw new InvalidOperationException($"Factory for {kind} returned null.");
                result.Add(instance);
            }
        }
        return result;
    }
}

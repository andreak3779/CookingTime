// filepath: Domain/Meals/MealTypeRegistry.cs
using CookingTime.Domain.Abstractions;

namespace CookingTime.Domain.Meals;

/// <summary>
/// Maps <see cref="MealKind"/> values to factory delegates. New kinds
/// register; they do not edit a switch. That is the OCP rule for meal
/// creation in this codebase.
/// </summary>
public sealed class MealTypeRegistry
{
    private readonly Dictionary<MealKind, Func<IMeal>> _factories = new();

    public void Register(MealKind kind, Func<IMeal> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factories[kind] = factory;
    }

    public bool TryCreate(MealKind kind, out IMeal meal)
    {
        if (_factories.TryGetValue(kind, out var factory))
        {
            var instance = factory()
                ?? throw new InvalidOperationException($"Factory for {kind} returned null.");
            meal = instance;
            return true;
        }
        meal = null!;
        return false;
    }

    public IReadOnlyCollection<MealKind> RegisteredKinds => _factories.Keys;
}

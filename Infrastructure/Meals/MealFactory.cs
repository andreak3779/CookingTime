// filepath: Infrastructure/Meals/MealFactory.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.Meals;
using CookingTime.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CookingTime.Infrastructure.Meals;

/// <summary>
/// Phase 3 production factory. Builds the catalog of meals from
/// <see cref="MealCatalogOptions"/>. Chicken and Turkey are always
/// registered from code (their cooking tables are canonical domain
/// knowledge); the remaining meals come from configuration.
/// </summary>
/// <remarks>
/// Phase 17: the catalog is materialised ONCE, in the constructor, and the
/// name index is built alongside it. Previously <see cref="CreateAll"/>
/// re-invoked every factory delegate on each call, and the application layer
/// called it on every calculation — so each "Calculate" rebuilt all twelve
/// meals. The meals are stateless, so one instance each is enough.
/// </remarks>
public sealed class MealFactory : IMealFactory
{
    private readonly IReadOnlyList<IMeal> _meals;

    /// <summary>
    /// Case-insensitive name index. Phase 17 also makes duplicate names
    /// impossible at the source: the Phase 15 validator rejects them at
    /// startup, and this dictionary would silently collapse them if it did
    /// not.
    /// </summary>
    private readonly Dictionary<string, IMeal> _byName;

    public MealFactory(IOptions<MealCatalogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var registry = BuildRegistry(options.Value);
        _meals = registry.CreateAll();
        _byName = new Dictionary<string, IMeal>(StringComparer.OrdinalIgnoreCase);

        foreach (var meal in _meals)
        {
            // First registration wins, matching the previous
            // FirstOrDefault-by-name behaviour exactly.
            _byName.TryAdd(meal.Name, meal);
        }
    }

    public IReadOnlyList<IMeal> CreateAll() => _meals;

    public bool TryGetByName(string name, out IMeal? meal)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            meal = null;
            return false;
        }

        return _byName.TryGetValue(name.Trim(), out meal);
    }

    private static MealTypeRegistry BuildRegistry(MealCatalogOptions options)
    {
        var registry = new MealTypeRegistry();
        registry.Register(MealKind.Chicken, () => new ChickenMeal());
        registry.Register(MealKind.Turkey, () => new TurkeyMeal());

        // Snapshot each definition so we don't capture the loop variable.
        foreach (var def in options.Catalog)
        {
            var local = def;
            if (local.Kind == MealKind.Chicken || local.Kind == MealKind.Turkey)
                continue; // already registered above
            registry.Register(MealKind.Range, () => BuildRangeMeal(local));
        }

        return registry;
    }

    private static IMeal BuildRangeMeal(MealDefinition def)
    {
        if (!def.MinMinutesPerPound.HasValue || !def.MaxMinutesPerPound.HasValue)
            throw new InvalidOperationException(
                $"Range meal '{def.Name}' must specify both MinMinutesPerPound and MaxMinutesPerPound.");
        return new RangeMeal(
            def.Name,
            def.Instructions,
            def.MinMinutesPerPound.Value,
            def.MaxMinutesPerPound.Value,
            def.MinPounds,
            def.MaxPounds);
    }
}

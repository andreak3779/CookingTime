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
public sealed class MealFactory : IMealFactory
{
    private readonly MealTypeRegistry _registry;

    public MealFactory(IOptions<MealCatalogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _registry = BuildRegistry(options.Value);
    }

    public IReadOnlyList<IMeal> CreateAll() => _registry.CreateAll();

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

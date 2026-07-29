// filepath: Infrastructure/Meals/MealFactoryStub.cs
using CookingTime.Domain.Abstractions;

namespace CookingTime.Infrastructure.Meals;

/// <summary>
/// Phase 2 placeholder. Returns an empty list of meals. The real
/// configuration-driven factory lands in Phase 3.
/// </summary>
public sealed class MealFactoryStub : IMealFactory
{
    public IReadOnlyList<IMeal> CreateAll() => Array.Empty<IMeal>();
}

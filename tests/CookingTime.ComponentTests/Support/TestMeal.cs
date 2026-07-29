// filepath: tests/CookingTime.ComponentTests/Support/TestMeal.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Domain.ValueObjects;

namespace CookingTime.ComponentTests.Support;

/// <summary>
/// Minimal <see cref="IMeal"/> implementation for component tests. Keeps
/// test code free of Moq plumbing for the domain objects.
/// </summary>
public sealed class TestMeal : IMeal
{
    public string Name { get; }
    public string Instructions { get; } = "";
    public MealKind Kind { get; }
    public Weight MinimumWeight { get; }
    public Weight MaximumWeight { get; }

    public TestMeal(string name, string instructions, MealKind kind, Weight min, Weight max)
    {
        Name = name;
        Instructions = instructions;
        Kind = kind;
        MinimumWeight = min;
        MaximumWeight = max;
    }

    public CookingDuration Calculate(Weight weight)
        => new(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(40));
}

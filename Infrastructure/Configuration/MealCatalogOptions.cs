// filepath: Infrastructure/Configuration/MealCatalogOptions.cs
using CookingTime.Domain.Abstractions;

namespace CookingTime.Infrastructure.Configuration;

/// <summary>
/// Configuration POCO bound from the "Meals" section of appsettings.json.
/// See <see cref="SectionName"/>.
/// </summary>
public sealed class MealCatalogOptions
{
    public const string SectionName = "Meals";
    public List<MealDefinition> Catalog { get; set; } = new();
}

/// <summary>
/// One meal definition. <see cref="Kind"/> chooses which fields are
/// populated: Chicken/Turkey use code-baked tables (only metadata here);
/// Range requires <see cref="MinMinutesPerPound"/> and
/// <see cref="MaxMinutesPerPound"/>.
/// </summary>
public sealed class MealDefinition
{
    public MealKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string Instructions { get; set; } = "";
    public decimal MinPounds { get; set; }
    public decimal MaxPounds { get; set; }
    public int? MinMinutesPerPound { get; set; }
    public int? MaxMinutesPerPound { get; set; }
}

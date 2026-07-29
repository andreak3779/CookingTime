// filepath: Infrastructure/Configuration/ProductInfoOptions.cs
namespace CookingTime.Infrastructure.Configuration;

/// <summary>
/// Configuration POCO bound from the "ProductInfo" section of appsettings.json.
/// Consumed by the About page (Phase 4).
/// </summary>
public sealed class ProductInfoOptions
{
    public const string SectionName = "ProductInfo";
    public string Name { get; set; } = "CookingTime";
    public string Version { get; set; } = "1.0.0";
    public string Company { get; set; } = "";
    public string CopyrightYear { get; set; } = "";
    public string ResumeUrl { get; set; } = "";
    public string Email { get; set; } = "";
}

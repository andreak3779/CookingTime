// filepath: Domain/Abstractions/IMealFactory.cs
namespace CookingTime.Domain.Abstractions;

/// <summary>
/// Produces the catalog of meals available to the application.
/// The Phase 2 implementation returns an empty list; the real
/// config-driven factory lands in Phase 3.
/// </summary>
public interface IMealFactory
{
    IReadOnlyList<IMeal> CreateAll();
}

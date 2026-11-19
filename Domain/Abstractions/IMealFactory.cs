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

    /// <summary>
    /// Finds a meal by name, case-insensitively.
    /// </summary>
    /// <param name="name">The meal name to look up.</param>
    /// <param name="meal">The matching meal, or <see langword="null"/> when
    /// no meal has that name.</param>
    /// <returns><see langword="true"/> when a match was found.</returns>
    /// <remarks>
    /// Phase 17: the caller used to do
    /// <c>CreateAll().FirstOrDefault(m =&gt; m.Name == name)</c>, which
    /// rebuilt the entire catalog — invoking every factory — on every
    /// calculation. The factory holds the catalog already, so it can answer
    /// the question directly.
    /// </remarks>
    bool TryGetByName(string name, out IMeal? meal);
}

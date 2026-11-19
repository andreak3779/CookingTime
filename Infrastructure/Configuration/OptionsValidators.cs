// filepath: Infrastructure/Configuration/OptionsValidators.cs
using CookingTime.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace CookingTime.Infrastructure.Configuration;

/// <summary>
/// Phase 15: startup validation for the options POCOs.
/// </summary>
/// <remarks>
/// <para>
/// The application bound its configuration with a plain
/// <c>services.Configure&lt;T&gt;(section)</c> and validated nothing. A
/// malformed catalog entry therefore failed <em>late</em> and in the wrong
/// place: <see cref="Meals.MealFactory"/> throws from its constructor, and the
/// factory is resolved lazily the first time the UI asks for the catalog. In
/// the browser that surfaces as an unhandled component exception on the
/// Cooking Time page, with no indication of which config line was wrong.
/// </para>
/// <para>
/// These validators move the failure to startup, where the message can name
/// the offending entry, and they are registered with
/// <c>ValidateOnStart()</c> so the application refuses to run with a bad
/// catalog rather than failing at first render.
/// </para>
/// </remarks>
public sealed class MealCatalogOptionsValidator : IValidateOptions<MealCatalogOptions>
{
    public ValidateOptionsResult Validate(string? name, MealCatalogOptions options)
    {
        var failures = new List<string>();

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < options.Catalog.Count; i++)
        {
            var def = options.Catalog[i];
            var where = $"Meals:Catalog[{i}]";

            if (string.IsNullOrWhiteSpace(def.Name))
            {
                failures.Add($"{where} must have a non-empty Name.");
                continue;
            }

            if (!seenNames.Add(def.Name))
            {
                // Duplicate names make meal lookup ambiguous: the catalog
                // lookup resolves by name, and the first match wins silently.
                failures.Add($"{where} has a duplicate Name '{def.Name}'. Meal names must be unique.");
            }

            if (def.Kind != MealKind.Range)
            {
                // Chicken and Turkey use code-baked cooking tables; only
                // metadata is read from configuration for them.
                continue;
            }

            if (!def.MinMinutesPerPound.HasValue || !def.MaxMinutesPerPound.HasValue)
            {
                failures.Add(
                    $"{where} ('{def.Name}') must specify both MinMinutesPerPound and MaxMinutesPerPound.");
            }
            else if (def.MinMinutesPerPound.Value <= 0 || def.MaxMinutesPerPound.Value <= 0)
            {
                failures.Add(
                    $"{where} ('{def.Name}') must specify positive MinMinutesPerPound and MaxMinutesPerPound.");
            }
            else if (def.MaxMinutesPerPound.Value < def.MinMinutesPerPound.Value)
            {
                failures.Add(
                    $"{where} ('{def.Name}') has MaxMinutesPerPound ({def.MaxMinutesPerPound.Value}) " +
                    $"less than MinMinutesPerPound ({def.MinMinutesPerPound.Value}); the range would be inverted.");
            }

            if (def.MinPounds <= 0M)
            {
                failures.Add($"{where} ('{def.Name}') must specify a positive MinPounds.");
            }

            if (def.MaxPounds <= 0M || def.MaxPounds < def.MinPounds)
            {
                failures.Add(
                    $"{where} ('{def.Name}') must specify MaxPounds greater than or equal to MinPounds.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}

/// <summary>
/// Phase 15: startup validation for <see cref="ProductInfoOptions"/>.
/// </summary>
/// <remarks>
/// The only field that needs a rule is <c>ResumeUrl</c>: it is rendered into
/// an <c>href</c> by the About page, and a <c>javascript:</c> value would be
/// a click-to-execute script URI (Razor's HTML-encoding does not neutralise
/// a scheme). An empty value is allowed - the page simply omits the link.
/// </remarks>
public sealed class ProductInfoOptionsValidator : IValidateOptions<ProductInfoOptions>
{
    public ValidateOptionsResult Validate(string? name, ProductInfoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ResumeUrl))
        {
            return ValidateOptionsResult.Success;
        }

        if (!options.TryGetSafeResumeUrl(out _))
        {
            return ValidateOptionsResult.Fail(
                $"ProductInfo:ResumeUrl ('{options.ResumeUrl}') is not an absolute http/https URI. " +
                "Only http and https are allowed; a value such as 'javascript:...' would be rendered " +
                "as a click-to-execute script URI.");
        }

        return ValidateOptionsResult.Success;
    }
}

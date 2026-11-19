// filepath: Infrastructure/Configuration/ProductInfoOptions.cs
using System.Diagnostics.CodeAnalysis;

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

    /// <summary>
    /// Phase 15: the schemes a <see cref="ResumeUrl"/> may use.
    ///
    /// The About page renders this value into an <c>href</c>. Razor HTML-encodes
    /// attribute values, but encoding does not neutralise a *scheme*: the
    /// payload <c>javascript:alert(1)</c> contains no character that needs
    /// encoding, so it reaches the browser verbatim as a script URI. This
    /// application is client-side only, so there is no server-side request
    /// forgery angle - but a poisoned config file would still turn the
    /// "Resume" link into a click-to-execute script.
    ///
    /// Only <c>http</c> and <c>https</c> are accepted. Everything else -
    /// <c>javascript:</c>, <c>data:</c>, <c>vbscript:</c>, <c>file:</c> - is
    /// rejected at startup so a bad value fails fast instead of rendering.
    /// </summary>
    private static readonly string[] AllowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];

    /// <summary>
    /// True when <see cref="ResumeUrl"/> is an absolute http/https URI that is
    /// safe to use as an <c>href</c>. An empty value is considered safe: the
    /// About page omits the link entirely when it is blank.
    /// </summary>
    public bool HasSafeResumeUrl() => TryGetSafeResumeUrl(out _);

    /// <summary>
    /// Attempts to read <see cref="ResumeUrl"/> as a safe, absolute http/https
    /// URI.
    /// </summary>
    /// <param name="url">The validated URL, or <see langword="null"/> when the
    /// configured value is absent or uses a disallowed scheme.</param>
    public bool TryGetSafeResumeUrl([NotNullWhen(true)] out string? url)
    {
        url = null;

        if (string.IsNullOrWhiteSpace(ResumeUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(ResumeUrl.Trim(), UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!AllowedSchemes.Contains(parsed.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        url = parsed.ToString();
        return true;
    }
}

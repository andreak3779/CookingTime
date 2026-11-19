// filepath: tests/CookingTime.UnitTests/ClientConfigurationDeliveryTests.cs
using FluentAssertions;

namespace CookingTime.UnitTests;

/// <summary>
/// Guards the client's configuration delivery path.
///
/// Phase 13 found that the Blazor WebAssembly client received **no
/// configuration at all**. The SDK only bundles configuration that lives in
/// <c>wwwroot/</c>:
/// <code>
/// &lt;_BlazorConfigFile Include="wwwroot\appsettings*.json" /&gt;
/// </code>
/// The repository kept <c>appsettings.json</c> at its root, so it was never
/// part of the client's boot manifest and the browser fell back to POCO
/// defaults — most visibly an **empty meal catalog**, leaving only the two
/// code-baked meals (Chicken, Turkey) in the dropdown.
///
/// Every other guard in this repo missed it:
///   - the build stays green (a misplaced config file is not a compile error),
///   - the server returns HTTP 200 for every route (it serves the SPA shell),
///   - bUnit injects a mocked <c>IMealFactory</c>, and
///   - the integration suite loads the file **server-side** from the content
///     root, never through the browser's boot manifest.
///
/// So the failure only surfaces in a real browser. These facts are the
/// automated proxy for that runtime invariant. See
/// docs/decisions/0008-no-personal-contact-data.md.
/// </summary>
public sealed class ClientConfigurationDeliveryTests
{
    /// <summary>
    /// The repository root, found by walking up from the test output directory
    /// until the production project file appears.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CookingTime.csproj")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root from '{AppContext.BaseDirectory}'.");
    }

    [Fact]
    public void Appsettings_LivesUnderWwwroot_SoTheSdkBundlesIt()
    {
        // Arrange
        var root = RepositoryRoot();
        var wwwrootConfig = Path.Combine(root, "wwwroot", "appsettings.json");

        // Act / Assert
        File.Exists(wwwrootConfig).Should().BeTrue(
            because: "the Blazor WASM SDK only bundles configuration matched by " +
                     "'wwwroot\\appsettings*.json'; a config file anywhere else never reaches the browser");
    }

    [Fact]
    public void Appsettings_DoesNotAlsoExistAtTheRepositoryRoot()
    {
        // Arrange
        var root = RepositoryRoot();
        var rootConfig = Path.Combine(root, "appsettings.json");

        // Act / Assert
        File.Exists(rootConfig).Should().BeFalse(
            because: "a root-level copy is not bundled by the SDK and creates a second, " +
                     "divergent source of truth for the same configuration");
    }

    [Fact]
    public void Appsettings_ContainsNoPersonalContactData()
    {
        // Arrange
        var root = RepositoryRoot();
        var path = Path.Combine(root, "wwwroot", "appsettings.json");
        var content = File.ReadAllText(path);

        // Act / Assert
        content.Should().NotContain("\"Email\"",
            because: "the file is served to every visitor now that it lives in wwwroot/; " +
                     "see docs/decisions/0008-no-personal-contact-data.md");
        content.Should().NotContain("@",
            because: "the served configuration file must not contain any email address");
    }

    /// <summary>
    /// Phase 15: the previous guard only looked for <c>@</c>, so a secret
    /// without an email-shaped payload — an API key, a bearer token, a
    /// private-key header — would have passed it. This widens the denylist to
    /// the shapes that actually matter in a *publicly served* file.
    /// </summary>
    [Theory]
    [InlineData("api_key", "api_key")]
    [InlineData("apiKey", "apikey")]
    [InlineData("apikey", "apikey")]
    [InlineData("password", "password")]
    [InlineData("passwd", "passwd")]
    [InlineData("secret", "secret")]
    [InlineData("bearer", "bearer")]
    [InlineData("-----BEGIN", "-----begin")]
    [InlineData("sk-", "sk-")]
    [InlineData("ghp_", "ghp_")]
    [InlineData("AKIA", "akia")]
    [InlineData("Authorization", "authorization")]
    [InlineData("client_secret", "client_secret")]
    public void Appsettings_ContainsNoSecretShapedValues(string pattern, string needle)
    {
        // Arrange
        var root = RepositoryRoot();
        var path = Path.Combine(root, "wwwroot", "appsettings.json");
        var content = File.ReadAllText(path);

        // Act / Assert
        content.ToLowerInvariant().Should().NotContain(needle,
            because: $"'{pattern}' is secret-shaped; the file is served to every visitor now that " +
                     "it lives in wwwroot/ and must never contain secrets - " +
                     "see docs/decisions/0008-no-personal-contact-data.md");
    }

    [Fact]
    public void Appsettings_IsStrictJson()
    {
        // Arrange
        var root = RepositoryRoot();
        var path = Path.Combine(root, "wwwroot", "appsettings.json");
        var content = File.ReadAllText(path);

        // Act / Assert
        content.Should().StartWith("{",
            because: "Phase 15: the file carried a leading '// filepath:' comment, which the .NET " +
                     "JSON provider tolerates but which is not valid JSON - it hides the file from " +
                     "any strict parser and from schema validation");
        content.TrimEnd().Should().EndWith("}",
            because: "the served configuration file must be a complete JSON document");

        var act = () => System.Text.Json.JsonDocument.Parse(content);
        act.Should().NotThrow("the served configuration file must be valid strict JSON");
    }

    [Fact]
    public void ProductionProject_HasNoRootLevelConfigCopy()
    {
        // Arrange
        var root = RepositoryRoot();
        var projectFile = Path.Combine(root, "CookingTime.csproj");
        var project = File.ReadAllText(projectFile);

        // Act / Assert
        project.Should().NotContain("\"appsettings.json\"",
            because: "the SDK default content glob already picks up wwwroot\\**; an explicit " +
                     "root-level entry would copy a stale config into the publish root, where a " +
                     "static-file server would expose it");
    }
}

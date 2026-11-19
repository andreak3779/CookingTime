// filepath: tests/CookingTime.UnitTests/BuildHardeningTests.cs
using System.Text.RegularExpressions;

namespace CookingTime.UnitTests;

/// <summary>
/// Phase 16: guards the supply-chain and release invariants that are easy
/// to regress silently.
/// </summary>
/// <remarks>
/// <para>
/// Each of these properties holds today and none of them is enforced by the
/// compiler. A future dependency bump, a CI edit, or a "just for CI" change
/// can quietly undo any of them, so they are pinned by tests rather than by
/// convention.
/// </para>
/// <para>
/// These are repository-shape assertions, not behaviour assertions — the
/// thing they protect is the build pipeline, not the application.
/// </para>
/// </remarks>
public sealed class BuildHardeningTests
{
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

    private static IEnumerable<string> ProjectFiles()
    {
        var root = RepositoryRoot();
        return Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}OriginalSource{Path.DirectorySeparatorChar}"));
    }

    [Fact]
    public void NoProject_UsesAFloatingPackageVersion()
    {
        // A floating version ("10.0.0-*") resolves to the highest patch at
        // restore time, so two builds of the same commit can pull different
        // packages. Phase 16 pinned every version exactly.
        var offenders = new List<string>();

        foreach (var project in ProjectFiles())
        {
            var content = File.ReadAllText(project);
            foreach (Match match in Regex.Matches(content, @"Version=""([^""]*\*)"""))
            {
                offenders.Add($"{Path.GetFileName(project)}: {match.Value}");
            }
        }

        offenders.Should().BeEmpty(
            because: "floating versions break reproducible builds - pin the exact version " +
                     "and commit the regenerated packages.lock.json");
    }

    [Fact]
    public void EveryProject_HasACommittedLockFile()
    {
        // The lock file pins the transitive graph, not just the direct
        // references. Without one, a transitive dependency can float.
        var missing = new List<string>();

        foreach (var project in ProjectFiles())
        {
            var lockFile = Path.Combine(
                Path.GetDirectoryName(project)!, "packages.lock.json");
            if (!File.Exists(lockFile))
            {
                missing.Add(Path.GetFileName(project));
            }
        }

        missing.Should().BeEmpty(
            because: "RestorePackagesWithLockFile writes packages.lock.json next to each project; " +
                     "it must be committed so the dependency graph is reviewable");
    }

    [Fact]
    public void EveryProject_RequestsTheLockFile()
    {
        // The lock file only gets written when the property is set, and the
        // test projects sit under tests\Directory.Build.props, which SHADOWS
        // the root one - so the property has to be set in both places.
        var rootProps = Path.Combine(RepositoryRoot(), "Directory.Build.props");
        var testProps = Path.Combine(RepositoryRoot(), "tests", "Directory.Build.props");

        foreach (var props in new[] { rootProps, testProps })
        {
            File.Exists(props).Should().BeTrue();
            File.ReadAllText(props).Should().Contain("<RestorePackagesWithLockFile>true",
                because: $"{Path.GetFileName(props)} must request the lock file; the tests/ props " +
                         "shadows the root one, so the property has to be set in both");
        }
    }

    [Fact]
    public void CI_PinsEveryActionToACommitSha()
    {
        // A tag can be moved to a different commit; a SHA cannot. Phase 16
        // pinned all four actions and added dependabot to keep them current.
        var workflow = Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(workflow);

        var uses = Regex.Matches(content, @"uses:\s*(?<action>[^\s@]+)@(?<ref>\S+)");

        uses.Should().NotBeEmpty();

        foreach (Match use in uses)
        {
            var action = use.Groups["action"].Value;
            var reference = use.Groups["ref"].Value;

            reference.Should().MatchRegex("^[0-9a-f]{40}$",
                because: $"{action} is pinned to a mutable tag ('{reference}'); a tag can be moved " +
                         "to a different commit, so pin it to the full commit SHA instead");
        }
    }

    [Fact]
    public void CI_HasADependabotConfig()
    {
        // SHA-pinned actions never upgrade themselves. Dependabot is what
        // keeps the pins from going stale.
        File.Exists(Path.Combine(RepositoryRoot(), ".github", "dependabot.yml"))
            .Should().BeTrue(
                because: "the pinned action SHAs need dependabot to propose updates, otherwise " +
                         "they stay frozen at the version pinned in Phase 16");
    }

    [Fact]
    public void CI_RestoresInLockedMode()
    {
        // Without --locked-mode, a restore that would change the dependency
        // graph silently regenerates the lock file and the drift goes
        // unnoticed. With it, the restore fails with NU1004.
        var workflow = Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(workflow);

        content.Should().Contain("dotnet restore --locked-mode",
            because: "the committed packages.lock.json is only binding when the restore runs in " +
                     "locked mode");
    }

    [Fact]
    public void CI_AuditsTheProductionProjectForVulnerablePackages()
    {
        // The test projects deliberately suppress NU1902/NU1903 for a
        // transitive bUnit/AngleSharp advisory, so only the project that
        // actually ships is gated.
        var workflow = Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(workflow);

        content.Should().Contain("dotnet list CookingTime.csproj package --vulnerable",
            because: "a known-vulnerable dependency in the shipped project must fail CI");
    }

    [Fact]
    public void CI_VerifiesThePublishOutputHasNoTestAssemblies()
    {
        // Publishing the SOLUTION copies xunit, Moq, bUnit, coverlet and all
        // three *Tests.dll files into the publish root. Only the app project
        // may ship, and this asserts the documented publish command stays
        // clean.
        var workflow = Path.Combine(RepositoryRoot(), ".github", "workflows", "ci.yml");
        var content = File.ReadAllText(workflow);

        content.Should().Contain("dotnet publish CookingTime.csproj",
            because: "the app project must be published explicitly - a solution-wide publish " +
                     "leaks every test assembly into the artifact");
        content.Should().Contain("*Tests*.dll",
            because: "the publish check must look for the leaked test assemblies");
    }

    [Fact]
    public void NuGetConfig_ClearsInheritedSourcesAndMapsPackages()
    {
        // Without <clear />, a machine-level NuGet.config could add a feed
        // and the build would differ from CI without anything in the repo
        // changing.
        var config = Path.Combine(RepositoryRoot(), "NuGet.config");
        File.Exists(config).Should().BeTrue();

        var content = File.ReadAllText(config);

        content.Should().Contain("<clear />",
            because: "inherited package sources must be cleared so every machine restores from the " +
                     "same feed");
        content.Should().Contain("<packageSourceMapping>",
            because: "package source mapping stops a package being pulled from an unexpected feed");
    }

    [Fact]
    public void GlobalJson_PinsTheSdk()
    {
        // Without it the SDK floats with whatever the machine has installed,
        // and CI installs "10.0.x" — the newest patch at run time.
        var globalJson = Path.Combine(RepositoryRoot(), "global.json");
        File.Exists(globalJson).Should().BeTrue();

        var content = File.ReadAllText(globalJson);
        content.Should().MatchRegex("\"version\"\\s*:\\s*\"\\d+\\.\\d+\\.\\d+\"",
            because: "the SDK version must be pinned to a specific version");
    }

    [Fact]
    public void TestProjects_UseTheSamePackageVersions()
    {
        // Two versions of the same library in one solution is a bug source:
        // the APIs drift, and a fix verified in one project silently does not
        // apply in the other.
        var versions = new Dictionary<string, HashSet<string>>();

        foreach (var project in ProjectFiles().Where(p => p.Contains("tests")))
        {
            var content = File.ReadAllText(project);
            foreach (Match match in Regex.Matches(content, @"Include=""(?<id>[^""]+)"" Version=""(?<v>[^""]+)"""))
            {
                var id = match.Groups["id"].Value;
                var version = match.Groups["v"].Value;

                if (!versions.TryGetValue(id, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    versions[id] = set;
                }

                set.Add(version);
            }
        }

        var mismatched = versions
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
            .ToList();

        mismatched.Should().BeEmpty(
            because: "the test projects must agree on package versions - " +
                     "a library at two versions in one solution makes test results misleading");
    }
}

// filepath: tests/CookingTime.UnitTests/PersonalDataLeakageTests.cs
using System.Text.RegularExpressions;

namespace CookingTime.UnitTests;

/// <summary>
/// Guards the repository against personal data leaking into published files.
/// </summary>
/// <remarks>
/// <para>
/// This repo is <strong>public</strong>. Everything committed to it is
/// downloadable, and a future commit can reintroduce personal data without
/// anyone noticing: the build stays green, the tests stay green, and a
/// reviewer scanning a diff for <em>logic</em> does not read a URL the way a
/// scraper does.
/// </para>
/// <para>
/// Two earlier sweeps in this repo both taught the same lesson:
/// </para>
/// <list type="bullet">
/// <item>
/// Phase 13 found a personal address in the served config and removed it
/// (ADR 0008).
/// </item>
/// <item>
/// The audit that produced <c>docs/plan-remove-pii.md</c> reported the
/// binary <c>CookingTime.suo</c> as clean, because its sweep was filtered on
/// the <em>email</em> pattern. The file contained no address - it contained a
/// Windows user-profile path, which the filter never looked for. It survived
/// two audits and was only found by an unfiltered sweep (ADR 0010).
/// </item>
/// </list>
/// <para>
/// So the guard below deliberately checks several <em>shapes</em>, and it
/// reads binary files as bytes rather than assuming they are clean because
/// they have no extension that looks like source. A guard that proves one
/// shape is absent proves only that shape is absent.
/// </para>
/// </remarks>
public sealed class PersonalDataLeakageTests
{
    /// <summary>
    /// Directories that never contain publishable content: build output,
    /// NuGet restore artifacts, and the test-results tree.
    /// </summary>
    private static readonly string[] ExcludedDirectoryNames =
    [
        ".git",
        "bin",
        "obj",
        "out",
        "TestResults",
    ];

    /// <summary>
    /// A real-looking email address: a local part, an at-sign, and a domain
    /// with a dot and a two-or-more letter TLD.
    /// </summary>
    private static readonly Regex EmailPattern = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    /// <summary>
    /// A Windows user-profile path. The legacy WinForms solution embedded the
    /// original developer's home directory in its IDE state file, which is
    /// exactly the kind of value that survives a "we removed the email" pass.
    /// </summary>
    private static readonly Regex WindowsUserProfilePattern = new(
        @"Documents\s+and\s+Settings",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// A POSIX home directory path. The owner's machine layout is not a secret,
    /// but it identifies a person and a workstation and has no business in a
    /// public repository.
    /// </summary>
    private static readonly Regex PosixHomePathPattern = new(
        @"/home/[A-Za-z0-9._\-]+/",
        RegexOptions.Compiled);

    /// <summary>
    /// Email addresses that are legitimate and must not trip the guard.
    /// </summary>
    private static readonly string[] AllowedEmailSubstrings =
    [
        // RFC 2606 reserved example domain - not a real address.
        "@example.com",
        // GitHub's noreply form - the identity this repo deliberately uses.
        "@users.noreply.github.com",
        // The repository's own public handle, referenced in docs and config.
        "@andreak3779",
    ];

    /// <summary>
    /// Path fragments that are legitimate and must not trip the guard. The
    /// GitHub Actions runner's own workspace path contains <c>/home/runner</c>,
    /// which is the build agent, not a person.
    /// </summary>
    private static readonly string[] AllowedPathSubstrings =
    [
        "/home/runner/",
    ];

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

    /// <summary>
    /// A single finding: the file it was found in and the offending value.
    /// </summary>
    private sealed record Finding(string File, string Detail);

    /// <summary>
    /// The scan result, computed once and shared by the three facts. The scan
    /// walks the whole repository, so repeating it per fact would multiply the
    /// cost for no benefit.
    /// </summary>
    private sealed record ScanResult(
        IReadOnlyList<Finding> Emails,
        IReadOnlyList<Finding> WindowsUserProfilePaths,
        IReadOnlyList<Finding> PosixHomePaths);

    private static readonly Lazy<ScanResult> Scan = new(ScanRepository, isThreadSafe: true);

    private static ScanResult ScanRepository()
    {
        var root = RepositoryRoot();

        var emails = new List<Finding>();
        var windowsPaths = new List<Finding>();
        var posixPaths = new List<Finding>();

        foreach (var path in PublishableFiles(root))
        {
            string content;
            try
            {
                content = ReadForScanning(path);
            }
            catch (IOException)
            {
                // A file that vanished or is locked mid-scan is not a finding.
                continue;
            }

            var relative = Path.GetRelativePath(root, path);

            foreach (Match match in EmailPattern.Matches(content))
            {
                if (AllowedEmailSubstrings.Any(allowed =>
                        match.Value.Contains(allowed, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                emails.Add(new Finding(relative, match.Value));
            }

            if (WindowsUserProfilePattern.IsMatch(content))
            {
                windowsPaths.Add(new Finding(relative, "Windows user-profile path"));
            }

            foreach (Match match in PosixHomePathPattern.Matches(content))
            {
                if (AllowedPathSubstrings.Any(allowed =>
                        match.Value.Contains(allowed, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                posixPaths.Add(new Finding(relative, match.Value));
            }
        }

        return new ScanResult(emails, windowsPaths, posixPaths);
    }

    /// <summary>
    /// Every file in the repository that could be published, excluding build
    /// output and VCS metadata.
    /// </summary>
    private static IEnumerable<string> PublishableFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !ExcludedDirectoryNames.Any(excluded =>
                path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                    .Contains(excluded, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Reads a file as text if it plausibly holds text, otherwise as bytes.
    /// Binary files are not skipped: the <c>.suo</c> finding is exactly the
    /// case where a binary file carried personal data.
    /// </summary>
    private static string ReadForScanning(string path)
    {
        var bytes = File.ReadAllBytes(path);

        // A file with no NUL byte in its first 8 KiB is treated as text. This
        // catches UTF-16 and other encodings without guessing at them.
        var probeLength = Math.Min(bytes.Length, 8192);
        var looksBinary = bytes.AsSpan(0, probeLength).IndexOf((byte)0) >= 0;

        return looksBinary
            ? System.Text.Encoding.Latin1.GetString(bytes)
            : System.Text.Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public void NoTrackedFile_ContainsARealEmailAddress()
    {
        // Act
        var offenders = Scan.Value.Emails;

        // Assert
        offenders.Should().BeEmpty(
            because: "this repository is public - a personal address in a tracked file is " +
                     "published to everyone who clones it. RFC 2606 example addresses and " +
                     "GitHub noreply addresses are exempt. See " +
                     "docs/decisions/0008-no-personal-contact-data.md");
    }

    [Fact]
    public void NoTrackedFile_ContainsAWindowsUserProfilePath()
    {
        // Act
        var offenders = Scan.Value.WindowsUserProfilePaths;

        // Assert
        offenders.Should().BeEmpty(
            because: "a Windows user-profile path embeds the original developer's home " +
                     "directory and account name. It was published for years inside the " +
                     "binary CookingTime.suo and survived two audits because the sweeps " +
                     "only looked for email-shaped strings - see " +
                     "docs/decisions/0010-redact-pii-in-original-source.md");
    }

    [Fact]
    public void NoTrackedFile_ContainsAPersonalPosixHomePath()
    {
        // Act
        var offenders = Scan.Value.PosixHomePaths;

        // Assert
        offenders.Should().BeEmpty(
            because: "the owner's local filesystem layout identifies a person and a " +
                     "workstation and has no business in a public repository");
    }

    [Fact]
    public void OriginalSource_DoesNotContainTheBinaryIdeStateFile()
    {
        // Arrange
        var suoPath = Path.Combine(
            RepositoryRoot(), "OriginalSource", "CookingTime.suo");

        // Act / Assert
        File.Exists(suoPath).Should().BeFalse(
            because: "CookingTime.suo is a Visual Studio 2005 solution-user file: it is " +
                     "excluded by the repository's own *.suo ignore rule, no project " +
                     "references it, and it carried a Windows user-profile path. It is " +
                     "regenerable IDE state, not source - see " +
                     "docs/decisions/0010-redact-pii-in-original-source.md");
    }

    [Fact]
    public void OriginalSource_AboutForm_CarriesNoContactStrings()
    {
        // Arrange
        var aboutPath = Path.Combine(
            RepositoryRoot(), "OriginalSource", "frmAbout.cs");

        // Act / Assert
        File.Exists(aboutPath).Should().BeTrue();

        var content = File.ReadAllText(aboutPath);

        content.Should().NotContain("mailto:",
            because: "the legacy About form published a third party's email address; " +
                     "Phase 19 emptied the link target");
        content.Should().NotContain("http://members.",
            because: "the legacy About form published a third party's personal homepage; " +
                     "Phase 19 emptied the link target");
    }

    [Fact]
    public void OriginalSource_AssemblyInfo_CarriesNoCompanyName()
    {
        // Arrange
        var assemblyInfoPath = Path.Combine(
            RepositoryRoot(), "OriginalSource", "AssemblyInfo.cs");

        // Act / Assert
        File.Exists(assemblyInfoPath).Should().BeTrue();

        var content = File.ReadAllText(assemblyInfoPath);

        content.Should().NotContain("AssemblyCompany(\"A",
            because: "the legacy assembly metadata carried a personal first name; " +
                     "Phase 19 emptied it");
    }
}

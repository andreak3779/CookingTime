// filepath: tests/CookingTime.UnitTests/Infrastructure/ProductInfoOptionsTests.cs
using CookingTime.Infrastructure.Configuration;

namespace CookingTime.UnitTests.Infrastructure;

/// <summary>
/// Phase 15: the <c>ResumeUrl</c> scheme allowlist.
/// </summary>
/// <remarks>
/// The About page renders this value into an <c>href</c>. Razor HTML-encodes
/// attribute values, but encoding does not neutralise a *scheme*: the payload
/// <c>javascript:alert(1)</c> contains no character that needs encoding, so
/// it reaches the browser verbatim as a script URI.
/// </remarks>
public sealed class ProductInfoOptionsTests
{
    [Theory]
    [InlineData("https://github.com/andreak3779/CookingTime")]
    [InlineData("http://example.com/resume")]
    [InlineData("https://example.com/path?query=1#frag")]
    public void TryGetSafeResumeUrl_HttpAndHttps_Succeeds(string url)
    {
        var options = new ProductInfoOptions { ResumeUrl = url };

        options.TryGetSafeResumeUrl(out var actual).Should().BeTrue();
        actual.Should().Be(url);
        options.HasSafeResumeUrl().Should().BeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(document.cookie)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("about:blank")]
    public void TryGetSafeResumeUrl_NonHttpSchemes_Fails(string url)
    {
        var options = new ProductInfoOptions { ResumeUrl = url };

        options.TryGetSafeResumeUrl(out var actual).Should().BeFalse();
        actual.Should().BeNull();
        options.HasSafeResumeUrl().Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetSafeResumeUrl_Blank_Fails(string url)
    {
        // A blank value is legitimate - the page simply omits the link.
        var options = new ProductInfoOptions { ResumeUrl = url };

        options.TryGetSafeResumeUrl(out var actual).Should().BeFalse();
        actual.Should().BeNull();
        options.HasSafeResumeUrl().Should().BeFalse();
    }

    [Theory]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    [InlineData("not a url at all")]
    public void TryGetSafeResumeUrl_NotAbsolute_Fails(string url)
    {
        var options = new ProductInfoOptions { ResumeUrl = url };

        options.TryGetSafeResumeUrl(out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetSafeResumeUrl_SurroundingWhitespace_IsTrimmed()
    {
        var options = new ProductInfoOptions { ResumeUrl = "  https://example.com/resume  " };

        options.TryGetSafeResumeUrl(out var actual).Should().BeTrue();
        actual.Should().Be("https://example.com/resume");
    }
}

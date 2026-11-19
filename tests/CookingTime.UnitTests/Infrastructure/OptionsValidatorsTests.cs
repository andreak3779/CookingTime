// filepath: tests/CookingTime.UnitTests/Infrastructure/OptionsValidatorsTests.cs
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CookingTime.UnitTests.Infrastructure;

/// <summary>
/// Phase 15: startup validation for the configuration POCOs.
/// </summary>
/// <remarks>
/// Before Phase 15 the catalog was bound with a plain
/// <c>services.Configure&lt;T&gt;</c> and validated nothing, so a malformed
/// entry failed at first render — as an unhandled component exception in the
/// browser, with no indication of which config line was wrong. These
/// validators move the failure to startup, where the message can name the
/// offending entry.
/// </remarks>
public sealed class OptionsValidatorsTests
{
    private static MealDefinition ValidRange(string name = "Pork Roast") => new()
    {
        Kind = MealKind.Range,
        Name = name,
        Instructions = "Cook it.",
        MinPounds = 1,
        MaxPounds = 24,
        MinMinutesPerPound = 40,
        MaxMinutesPerPound = 45,
    };

    private static ValidateOptionsResult ValidateCatalog(params MealDefinition[] catalog)
    {
        var options = new MealCatalogOptions();
        options.Catalog.AddRange(catalog);
        return new MealCatalogOptionsValidator().Validate(null, options);
    }

    [Fact]
    public void Validate_ValidCatalog_Succeeds()
    {
        ValidateCatalog(ValidRange()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCatalog_Succeeds()
    {
        // Chicken and Turkey are code-baked; an empty catalog is legitimate.
        ValidateCatalog().Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_BlankName_Fails()
    {
        var def = ValidRange();
        def.Name = "  ";

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("non-empty Name");
    }

    [Fact]
    public void Validate_DuplicateNames_Fails()
    {
        // Duplicate names make the catalog lookup ambiguous - the first match
        // would win silently.
        var result = ValidateCatalog(ValidRange("Pork Roast"), ValidRange("Pork Roast"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("duplicate Name");
    }

    [Fact]
    public void Validate_DuplicateNames_CaseInsensitive_Fails()
    {
        var result = ValidateCatalog(ValidRange("Pork Roast"), ValidRange("PORK ROAST"));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("duplicate Name");
    }

    [Fact]
    public void Validate_RangeMealMissingMinutes_Fails()
    {
        var def = ValidRange();
        def.MaxMinutesPerPound = null;

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("must specify both MinMinutesPerPound and MaxMinutesPerPound");
    }

    [Fact]
    public void Validate_NonPositiveMinutes_Fails()
    {
        var def = ValidRange();
        def.MinMinutesPerPound = 0;

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("positive MinMinutesPerPound");
    }

    [Fact]
    public void Validate_InvertedMinutesRange_Fails()
    {
        // An inverted per-pound range produces a duration whose minimum
        // exceeds its maximum - the Phase 14 defect class, one layer up.
        var def = ValidRange();
        def.MinMinutesPerPound = 45;
        def.MaxMinutesPerPound = 40;

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("less than MinMinutesPerPound");
    }

    [Fact]
    public void Validate_NonPositiveMinPounds_Fails()
    {
        var def = ValidRange();
        def.MinPounds = 0;

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("positive MinPounds");
    }

    [Fact]
    public void Validate_MaxPoundsBelowMinPounds_Fails()
    {
        var def = ValidRange();
        def.MinPounds = 10;
        def.MaxPounds = 5;

        var result = ValidateCatalog(def);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxPounds greater than or equal to MinPounds");
    }

    [Fact]
    public void Validate_ChickenKind_IgnoresRangeFields()
    {
        // Chicken and Turkey use code-baked cooking tables; only metadata is
        // read from configuration, so the range rules must not apply.
        var def = new MealDefinition
        {
            Kind = MealKind.Chicken,
            Name = "Chicken",
            Instructions = "Cook it.",
        };

        ValidateCatalog(def).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReportsTheOffendingIndex()
    {
        var bad = ValidRange("Bad Meal");
        bad.MaxMinutesPerPound = null;

        var result = ValidateCatalog(ValidRange(), bad);

        result.FailureMessage.Should().Contain("Meals:Catalog[1]");
    }

    [Fact]
    public void ProductInfo_EmptyResumeUrl_Succeeds()
    {
        var options = new ProductInfoOptions { ResumeUrl = "" };

        new ProductInfoOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ProductInfo_HttpResumeUrl_Succeeds()
    {
        var options = new ProductInfoOptions { ResumeUrl = "http://example.com/resume" };

        new ProductInfoOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ProductInfo_HttpsResumeUrl_Succeeds()
    {
        var options = new ProductInfoOptions { ResumeUrl = "https://example.com/resume" };

        new ProductInfoOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(document.cookie)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("file:///etc/passwd")]
    [InlineData("/relative/path")]
    [InlineData("not a url at all")]
    public void ProductInfo_UnsafeResumeUrl_Fails(string url)
    {
        // Phase 15: Razor HTML-encodes attribute values but does not
        // neutralise a *scheme*, so "javascript:..." reaches the browser as a
        // script URI. Only http/https may reach an href.
        var options = new ProductInfoOptions { ResumeUrl = url };

        var result = new ProductInfoOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("absolute http/https URI");
    }
}

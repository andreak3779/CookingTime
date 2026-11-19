// filepath: tests/CookingTime.ComponentTests/AboutRazorTests.cs
using CookingTime.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CookingTime.ComponentTests;

public sealed class AboutRazorTests : TestContext
{
    [Fact]
    public void RendersProductInfo_FromOptions()
    {
        Services.Configure<ProductInfoOptions>(o =>
        {
            o.Name = "CookingTime";
            o.Version = "2.0.0";
            o.Company = "CookingTime Inc.";
            o.CopyrightYear = "2026";
            o.ResumeUrl = "https://github.com/example/CookingTime";
        });

        var ctx = RenderComponent<Pages.About>();
        var markup = ctx.Markup;

        markup.Should().Contain("CookingTime 2.0.0");
        markup.Should().Contain("CookingTime Inc.");
        markup.Should().Contain("2026");
        markup.Should().Contain("https://github.com/example/CookingTime");
    }

    [Fact]
    public void RendersDefaults_WhenOptionsNotConfigured()
    {
        Services.Configure<ProductInfoOptions>(o =>
        {
            // empty options
            o.Name = "";
            o.Version = "";
            o.Company = "";
            o.CopyrightYear = "";
            o.ResumeUrl = "";
        });

        var ctx = RenderComponent<Pages.About>();
        // Should render without throwing, and no contact link should appear.
        ctx.Markup.Should().NotContain("mailto:");
    }

    [Fact]
    public void ProductInfoOptions_HasNoEmailProperty()
    {
        // The Email option was removed in Phase 13 so the app stops publishing
        // a mailto: address. Guard against it being reintroduced.
        typeof(ProductInfoOptions)
            .GetProperties()
            .Should().NotContain(p => p.Name == "Email");
    }
}

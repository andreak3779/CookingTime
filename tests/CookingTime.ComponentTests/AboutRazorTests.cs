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
            o.Email = "dev@example.com";
        });

        var ctx = RenderComponent<Pages.About>();
        var markup = ctx.Markup;

        markup.Should().Contain("CookingTime 2.0.0");
        markup.Should().Contain("CookingTime Inc.");
        markup.Should().Contain("2026");
        markup.Should().Contain("dev@example.com");
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
            o.Email = "";
        });

        var ctx = RenderComponent<Pages.About>();
        // Should render without throwing, and the email link should not appear.
        ctx.Markup.Should().NotContain("mailto:");
    }
}

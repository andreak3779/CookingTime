// filepath: tests/CookingTime.ComponentTests/IndexRazorTests.cs
namespace CookingTime.ComponentTests;

public sealed class IndexRazorTests : TestContext
{
    [Fact]
    public void SmokeRender_ContainsWelcomeAndLink()
    {
        var ctx = RenderComponent<Pages.Index>();
        ctx.Markup.Should().Contain("Welcome");
        ctx.Markup.Should().Contain("/cooking-time");
    }
}

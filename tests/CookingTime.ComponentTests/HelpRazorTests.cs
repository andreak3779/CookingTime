// filepath: tests/CookingTime.ComponentTests/HelpRazorTests.cs
namespace CookingTime.ComponentTests;

public sealed class HelpRazorTests : TestContext
{
    [Fact]
    public void Lists_AllFour_LegacyHelpFiles()
    {
        var ctx = RenderComponent<Pages.Help>();
        var links = ctx.FindAll("a");

        var hrefs = links.Select(a => a.GetAttribute("href")).Where(h => h is not null).ToList();
        hrefs.Should().Contain("/help/CookingInstructionsHelp.htm");
        hrefs.Should().Contain("/help/Calculatinghelp.htm");
        hrefs.Should().Contain("/help/Resethelp.htm");
        hrefs.Should().Contain("/help/YourCookingaHelp.htm");
    }
}

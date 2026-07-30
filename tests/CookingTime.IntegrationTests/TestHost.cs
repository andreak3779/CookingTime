// filepath: tests/CookingTime.IntegrationTests/TestHost.cs
using CookingTime.Application;

namespace CookingTime.IntegrationTests;

/// <summary>
/// The test host's WebApplication pipeline. Lives in a non-<c>Program</c>
/// class to avoid colliding with the Main that
/// <c>Microsoft.NET.Test.Sdk</c> auto-generates for the test project.
/// <see cref="WebApplicationFactory{TEntryPoint}"/> binds to the
/// <c>Marker</c> type in this assembly, not the host class.
/// </summary>
public static class TestHost
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Same composition root as the WASM client. Anything new goes in
        // Application/ServiceCollectionExtensions.AddCookingTime, not here.
        builder.Services.AddCookingTime(builder.Configuration);

        var app = builder.Build();

        // Serve the WASM client. The Blazor SDK on the client project
        // emits its compiled output to bin/<Config>/<TFM>/wwwroot of THIS
        // project (via ProjectReference), which is what
        // UseBlazorFrameworkFiles picks up. UseStaticFiles makes the
        // wwwroot/help/*.htm assets addressable over HTTP just like in
        // production.
        app.UseStaticFiles();
        app.UseBlazorFrameworkFiles();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}

// filepath: Program.cs
using CookingTime;
using CookingTime.Application;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Composition root is shared with the Phase 5 test-only host
// (tests/CookingTime.IntegrationTests/Hosting). Anything new goes
// in Application/ServiceCollectionExtensions.AddCookingTime, not here.
builder.Services.AddCookingTime(builder.Configuration);

await builder.Build().RunAsync();

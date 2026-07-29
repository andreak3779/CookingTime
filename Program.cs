// filepath: Program.cs
using CookingTime;
using CookingTime.Application.Abstractions;
using CookingTime.Application.Services;
using CookingTime.Domain.Abstractions;
using CookingTime.Infrastructure.Configuration;
using CookingTime.Infrastructure.Meals;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Options binding
builder.Services.Configure<MealCatalogOptions>(
    builder.Configuration.GetSection(MealCatalogOptions.SectionName));
builder.Services.Configure<ProductInfoOptions>(
    builder.Configuration.GetSection(ProductInfoOptions.SectionName));

// Application services (Singleton — both are stateless, and WASM has
// no per-request scope anyway).
builder.Services.AddSingleton<IMealFactory, MealFactory>();
builder.Services.AddSingleton<ICookingTimeCalculator, CookingTimeCalculator>();

await builder.Build().RunAsync();

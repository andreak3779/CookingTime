// filepath: tests/CookingTime.IntegrationTests/Marker.cs
namespace CookingTime.IntegrationTests;

/// <summary>
/// Anchor type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// The factory needs a real <c>Program</c>-equivalent class to bind to, and we
/// want to keep the test host separate from the WASM client's entry point so
/// that production <c>Program.cs</c> stays untouched.
/// </summary>
public sealed class Marker;

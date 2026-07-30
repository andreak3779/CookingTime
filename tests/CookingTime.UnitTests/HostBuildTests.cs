// filepath: tests/CookingTime.UnitTests/HostBuildTests.cs
using System.Reflection;
using FluentAssertions;

namespace CookingTime.UnitTests;

public sealed class HostBuildTests
{
    [Fact]
    public void MainAssembly_LoadsSuccessfully()
    {
        // Arrange / Act
        var assembly = typeof(Program).Assembly;

        // Assert
        assembly.Should().NotBeNull();
        assembly.GetName().Name.Should().Be("CookingTime");
    }

    [Fact]
    public void MainAssembly_TargetsNet10()
    {
        // Arrange / Act
        var assembly = typeof(Program).Assembly;

        // Assert
        assembly.GetCustomAttributes<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Single().FrameworkName
            .Should().StartWith(".NETCoreApp,Version=v10.0");
    }

    [Fact]
    public void Program_EntryPointType_IsAccessible()
    {
        // Arrange
        var programType = typeof(Program);

        // Assert
        programType.Should().NotBeNull();
        programType.Name.Should().Be("Program");
        programType.Assembly.GetName().Name.Should().Be("CookingTime");
    }
}

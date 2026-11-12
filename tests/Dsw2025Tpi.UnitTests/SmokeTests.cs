using FluentAssertions;

namespace Dsw2025Tpi.UnitTests;

public class SmokeTests
{
    [Fact]
    public void TestProject_IsDiscoveredAndRuns()
    {
        true.Should().BeTrue();
    }
}

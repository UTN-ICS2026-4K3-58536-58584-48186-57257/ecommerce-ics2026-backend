using System.Net;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class SmokeTests
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Healthcheck_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthcheck");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class FactoryExtensionsTests
{
    // Nombre exacto del rol en la API ([Authorize(Roles = "admin")] y UserSeeder).
    private const string AdminRole = "admin";

    private readonly CustomWebApplicationFactory _factory;

    public FactoryExtensionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateApiClient_WithAdminRole_CanAccessAdminEndpoint()
    {
        using var client = _factory.CreateApiClient(AdminRole);

        var response = await client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApiClient_WithoutRole_IsUnauthorizedOnAdminEndpoint()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/products/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

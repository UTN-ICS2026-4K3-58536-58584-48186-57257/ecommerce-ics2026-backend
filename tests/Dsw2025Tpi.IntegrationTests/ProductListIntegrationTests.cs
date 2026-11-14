using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class ProductListIntegrationTests
{
    private const string PublicUrl = "/api/products";
    private const string AdminUrl = "/api/products/admin";
    private readonly CustomWebApplicationFactory _factory;

    public ProductListIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------
    // 1) Endpoint publico: sin credenciales, 200 (o 204) y estructura paginada
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetProducts_PublicEndpoint_ReturnsOkWithPaginatedJsonStructure()
    {
        // Arrange: cliente anonimo y al menos un producto activo en la base
        var client = _factory.CreateApiClient();
        await SeedProductAsync($"Producto publico {NewMarker()}", isActive: true);

        // Act
        var response = await client.GetAsync(PublicUrl);

        // Assert
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent,
            $"Se esperaba 200 o 204 pero fue {(int)response.StatusCode}.");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductModel.ProductResponse>>();
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Items);
        Assert.True(body.Total >= 1);
    }

    // ---------------------------------------------------------------
    // 2) Query string: searchTerm, pageNumber y pageSize llegan a la base real
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetProducts_WithQueryStringFilters_ReturnsFilteredResultsFromSqlite()
    {
        // Arrange: 3 productos, solo uno contiene el marcador unico en el nombre
        var client = _factory.CreateApiClient();
        var marker = NewMarker();
        await SeedProductAsync($"Mouse Gamer {marker}", isActive: true);
        await SeedProductAsync("Teclado Mecanico", isActive: true);
        await SeedProductAsync("Monitor", isActive: true);

        // Se busca en MAYUSCULAS para comprobar tambien que ignora el caso
        var url = $"{PublicUrl}?searchTerm={marker.ToUpperInvariant()}&pageNumber=1&pageSize=10";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductModel.ProductResponse>>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Total);

        var item = Assert.Single(body.Items);
        Assert.Equal($"Mouse Gamer {marker}", item.Name);
    }

    // ---------------------------------------------------------------
    // 3) Endpoint admin: con token admin incluye productos inactivos
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetAuthProducts_AdminEndpoint_WhenCalledByAdmin_IncludesInactiveProducts()
    {
        // Arrange: un producto activo y uno inactivo con el mismo marcador
        var client = _factory.CreateApiClient("admin");
        var marker = NewMarker();
        await SeedProductAsync($"Activo {marker}", isActive: true);
        var inactiveSku = await SeedProductAsync($"Inactivo {marker}", isActive: false);

        // Act
        var response = await client.GetAsync($"{AdminUrl}?searchTerm={marker}&pageNumber=1&pageSize=50");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductModel.ProductResponse>>();
        Assert.NotNull(body);
        Assert.Contains(body!.Items, p => p.Sku == inactiveSku && !p.IsActive);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    // Texto unico por test: aisla los datos aunque la base se comparta en la coleccion
    private static string NewMarker() => Guid.NewGuid().ToString("N")[..8];

    // Inserta un producto en SQLite y devuelve su SKU
    private async Task<string> SeedProductAsync(string name, bool isActive)
    {
        var sku = $"SKU-{Guid.NewGuid():N}";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            InternalCode = $"INT-{Guid.NewGuid():N}",
            Name = name,
            Description = "Producto sembrado por el test",
            CurrentUnitPrice = 100m,
            StockQuantity = 10,
            IsActive = isActive
        });
        await db.SaveChangesAsync();

        return sku;
    }
}
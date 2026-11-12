using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

[Collection(IntegrationTestsCollection.Name)]
public class ProductCreateIntegrationTests
{
    private const string Url = "/api/products";
    private readonly CustomWebApplicationFactory _factory;

    public ProductCreateIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------
    // 1) Anónimo -> 401 / rol no admin -> 403
    // ---------------------------------------------------------------
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    public async Task AddProduct_WhenUnauthenticatedOrNotAdmin_ReturnsUnauthorizedOrForbidden(
        string? role, HttpStatusCode expectedStatus)
    {
        // Arrange
        var client = _factory.CreateApiClient(role);
        var sku = NewSku();

        // Act
        var response = await client.PostAsJsonAsync(Url, NewRequest(sku));

        // Assert
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.False(await ExistsInDbAsync(sku), "No debería haberse guardado el producto.");
    }

    // ---------------------------------------------------------------
    // 2) Admin -> 201 y la fila existe en SQLite con los valores correctos (ROTO A PROPÓSITO)
    // ---------------------------------------------------------------
    [Fact]
    public async Task AddProduct_WhenAuthenticatedAsAdmin_PersistsInSqliteAndReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateApiClient("admin");
        var sku = NewSku();
        var request = NewRequest(sku);

        // Act
        var response = await client.PostAsJsonAsync(Url, request);

        // Assert (respuesta HTTP)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<ProductModel.ProductResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);

        // Assert (fila real en SQLite)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        var row = await db.Products.AsNoTracking().SingleAsync(p => p.Sku == sku);

        Assert.Equal(body.Id, row.Id);
        Assert.Equal(request.InternalCode, row.InternalCode);
        Assert.Equal(request.Name, row.Name);
        Assert.Equal(request.Description, row.Description);
        
        // --- AQUÍ ESTÁ EL CAMBIO PARA ROMPER EL TEST ---
        // Se espera un precio incorrecto (9999.99) en lugar de 1200.50 para forzar el fallo
        Assert.Equal(1200.50m, row.CurrentUnitPrice); 
        
        Assert.Equal(request.StockQuantity, row.StockQuantity);
        Assert.True(row.IsActive);
    }

    // ---------------------------------------------------------------
    // 3) SKU duplicado -> 409 (o 400), nunca 500
    // ---------------------------------------------------------------
    [Fact]
    public async Task AddProduct_WhenSkuAlreadyExistsInDatabase_ReturnsConflictStatusCode()
    {
        // Arrange: el producto ya existe en la base
        var client = _factory.CreateApiClient("admin");
        var sku = NewSku();
        await SeedProductAsync(sku);

        // Act
        var response = await client.PostAsJsonAsync(Url, NewRequest(sku));

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest,
            $"Se esperaba 409 o 400 pero fue {(int)response.StatusCode}.");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ErrorMessages.SkuAlreadyExists, json.RootElement.GetProperty("error").GetString());

        // No se duplicó la fila
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        Assert.Equal(1, await db.Products.CountAsync(p => p.Sku == sku));
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private static string NewSku() => $"SKU-{Guid.NewGuid():N}";

    private static ProductModel.ProductRequest NewRequest(string sku) =>
        new(sku, "INT-001", "Notebook 14", "Notebook de prueba", 1200.50m, 10);

    private async Task<bool> ExistsInDbAsync(string sku)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        return await db.Products.AnyAsync(p => p.Sku == sku);
    }

    private async Task SeedProductAsync(string sku)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            InternalCode = "INT-EXISTENTE",
            Name = "Producto existente",
            Description = "Ya cargado en la base",
            CurrentUnitPrice = 500m,
            StockQuantity = 5,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
}

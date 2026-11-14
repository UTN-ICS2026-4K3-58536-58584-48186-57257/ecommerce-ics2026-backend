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
public class OrdersControllerTests
{
    private const string Url = "/api/orders";
    private readonly CustomWebApplicationFactory _factory;

    public OrdersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------
    // 1) Sin iniciar sesión -> 401
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateOrder_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateApiClient(); // sin rol = sin token
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(price: 100m, stock: 10);

        // Act
        var response = await client.PostAsJsonAsync(Url, NewRequest(customerId, productId, quantity: 1));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, await CountOrdersAsync(customerId));
    }

    // ---------------------------------------------------------------
    // 2) Customer con datos válidos -> 201, la orden se guarda y el stock baja
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateOrder_WhenAuthenticatedWithValidData_ReturnsCreatedAndPersistsOrderAndStockInSqlite()
    {
        // Arrange
        var client = _factory.CreateApiClient("customer");
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(price: 100m, stock: 10);

        // Act
        var response = await client.PostAsJsonAsync(Url, NewRequest(customerId, productId, quantity: 3));

        // Assert (respuesta HTTP)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<OrderModel.OrderResponse>();
        Assert.NotNull(body);

        // Assert (datos reales en SQLite)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();

        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .SingleAsync(o => o.Id == body!.Id);

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(300m, order.TotalAmount);   // 3 x 100
        Assert.Single(order.OrderItems);

        var product = await db.Products.AsNoTracking().SingleAsync(p => p.Id == productId);
        Assert.Equal(7, product.StockQuantity);  // 10 - 3
    }

    // ---------------------------------------------------------------
    // 3) Stock insuficiente -> 400 con mensaje de error (nunca 500)
    // ---------------------------------------------------------------
    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ReturnsBadRequestWithErrorMessage()
    {
        // Arrange
        var client = _factory.CreateApiClient("customer");
        var customerId = await SeedCustomerAsync();
        var productId = await SeedProductAsync(price: 100m, stock: 2);

        // Act
        var response = await client.PostAsJsonAsync(Url, NewRequest(customerId, productId, quantity: 5));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ErrorMessages.InsufficientStock, json.RootElement.GetProperty("error").GetString());

        Assert.Equal(0, await CountOrdersAsync(customerId));
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private static OrderModel.OrderRequest NewRequest(Guid customerId, Guid productId, int quantity) =>
        new(customerId,
            "Calle Falsa 123",
            "Calle Falsa 123",
            new List<OrderModel.OrderItemRequest> { new(productId, quantity) });

    private async Task<Guid> SeedCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Cliente de Prueba",
            EMail = $"cliente-{Guid.NewGuid():N}@test.com",
            PhoneNumber = "3810000000"
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<Guid> SeedProductAsync(decimal price, int stock)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = $"SKU-{Guid.NewGuid():N}",
            InternalCode = "INT-ORDEN",
            Name = "Producto para orden",
            Description = "Producto de prueba",
            CurrentUnitPrice = price,
            StockQuantity = stock,
            IsActive = true
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    private async Task<int> CountOrdersAsync(Guid customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        return await db.Orders.CountAsync(o => o.CustomerId == customerId);
    }
}

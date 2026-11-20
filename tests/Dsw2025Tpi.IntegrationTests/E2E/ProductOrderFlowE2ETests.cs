using System.Net;
using System.Net.Http.Json;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests.E2E;

[Collection(IntegrationTestsCollection.Name)]
public class ProductOrderFlowE2ETests
{
    private const string ProductsUrl = "/api/products";
    private const string AdminProductsUrl = "/api/products/admin";
    private const string OrdersUrl = "/api/orders";

    private readonly CustomWebApplicationFactory _factory;

    public ProductOrderFlowE2ETests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------
    // Flujo completo: admin crea -> público busca -> customer compra -> admin ve el stock actualizado
    // ---------------------------------------------------------------
    [Fact]
    public async Task ProductLifecycle_CreateSearchAndOrder_UpdatesStockEndToEnd()
    {
        // Arrange
        var adminClient = _factory.CreateApiClient("admin");
        var anonymousClient = _factory.CreateApiClient();
        var customerClient = _factory.CreateApiClient("customer");

        var sku = $"SKU-{Guid.NewGuid():N}";
        var productRequest = new ProductModel.ProductRequest(
            sku, $"INT-{Guid.NewGuid():N}", "Notebook E2E", "Producto del flujo E2E", 100m, 10);

        // El cliente no tiene endpoint de alta propio: se siembra igual que en OrdersControllerTests
        var customerId = await SeedCustomerAsync();

        // Act 1: el admin crea el producto
        var createResponse = await adminClient.PostAsJsonAsync(ProductsUrl, productRequest);

        // Assert 1
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductModel.ProductResponse>();
        Assert.NotNull(created);
        Assert.Equal(sku, created!.Sku);
        Assert.Equal(10, created.StockQuantity);

        // Act 2: sin token, se busca el producto por SKU en el listado público
        var searchResponse = await anonymousClient.GetAsync($"{ProductsUrl}?searchTerm={sku}");

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchBody = await searchResponse.Content.ReadFromJsonAsync<PagedResult<ProductModel.ProductResponse>>();
        Assert.NotNull(searchBody);
        var found = Assert.Single(searchBody!.Items, p => p.Sku == sku);
        Assert.Equal(created.Id, found.Id);

        // Act 3: el customer compra 3 unidades
        var orderRequest = new OrderModel.OrderRequest(
            customerId,
            "Calle Falsa 123",
            "Calle Falsa 123",
            new List<OrderModel.OrderItemRequest> { new(created.Id, 3) });
        var orderResponse = await customerClient.PostAsJsonAsync(OrdersUrl, orderRequest);

        // Assert 3
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderModel.OrderResponse>();
        Assert.NotNull(order);

        // Act 4: el admin consulta el listado de administración
        var adminListResponse = await adminClient.GetAsync($"{AdminProductsUrl}?searchTerm={sku}");

        // Assert 4: el stock bajó de 10 a 7
        Assert.Equal(HttpStatusCode.OK, adminListResponse.StatusCode);
        var adminBody = await adminListResponse.Content.ReadFromJsonAsync<PagedResult<ProductModel.ProductResponse>>();
        Assert.NotNull(adminBody);
        var updated = Assert.Single(adminBody!.Items, p => p.Sku == sku);
        Assert.Equal(7, updated.StockQuantity);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private async Task<Guid> SeedCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Cliente E2E",
            EMail = $"cliente-e2e-{Guid.NewGuid():N}@test.com",
            PhoneNumber = "3810000000"
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }
}

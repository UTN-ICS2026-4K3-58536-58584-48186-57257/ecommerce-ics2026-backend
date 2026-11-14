using System.Linq.Expressions;
using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;

namespace Dsw2025Tpi.UnitTests;

public class OrderManagementServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly OrderManagementService _service;

    public OrderManagementServiceTests()
    {
        // Repositorio "de mentira": no toca ninguna base de datos
        _repositoryMock = new Mock<IRepository>();

        // CreateOrderAsync solo usa el repositorio, por eso el contexto va en null
        _service = new OrderManagementService(null!, _repositoryMock.Object);
    }

    // ---------- Ayudas para preparar los datos ----------

    private static Customer CreateCustomer(Guid id) => new Customer
    {
        Id = id,
        Name = "Cliente de Prueba",
        EMail = "cliente@test.com",
        PhoneNumber = "3810000000"
    };

    private static Product CreateProduct(string name, decimal price, int stock) => new Product
    {
        Id = Guid.NewGuid(),
        Sku = $"SKU-{name}",
        InternalCode = $"INT-{name}",
        Name = name,
        Description = $"Descripción de {name}",
        CurrentUnitPrice = price,
        StockQuantity = stock,
        IsActive = true
    };

    private void SetupCustomer(Guid customerId, Customer? customer)
    {
        _repositoryMock
            .Setup(r => r.GetById<Customer>(customerId, It.IsAny<string[]>()))
            .ReturnsAsync(customer);
    }

    private void SetupProducts(params Product[] products)
    {
        // El repositorio falso busca el producto pedido dentro de esta lista
        _repositoryMock
            .Setup(r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, string[] _) =>
                products.AsQueryable().FirstOrDefault(predicate));
    }

    private void SetupSaveOperations()
    {
        _repositoryMock
            .Setup(r => r.Update(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o);
    }

    // ---------- Tests ----------

    [Fact]
    public async Task CreateOrder_WithValidItemsAndStock_CalculatesTotalAndDecrementsProductStock()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        SetupCustomer(customerId, CreateCustomer(customerId));

        var mouse = CreateProduct("Mouse", price: 100m, stock: 10);
        var teclado = CreateProduct("Teclado", price: 50m, stock: 20);
        SetupProducts(mouse, teclado);
        SetupSaveOperations();

        var request = new OrderModel.OrderRequest(
            customerId,
            "Calle Falsa 123",
            "Calle Falsa 123",
            new List<OrderModel.OrderItemRequest>
            {
                new(mouse.Id, 2),   // 2 x 100 = 200
                new(teclado.Id, 3)  // 3 x 50  = 150
            });

        // Act
        var result = await _service.CreateOrderAsync(request);

        // Assert
        // El stock se descontó
        Assert.Equal(8, mouse.StockQuantity);
        Assert.Equal(17, teclado.StockQuantity);

        // Se pidió al repositorio actualizar cada producto una vez
        _repositoryMock.Verify(r => r.Update(mouse), Times.Once);
        _repositoryMock.Verify(r => r.Update(teclado), Times.Once);

        // El total es la suma exacta de los subtotales (200 + 150)
        Assert.Equal(350m, result.TotalAmount);
        Assert.Equal(result.Items.Sum(i => i.Subtotal), result.TotalAmount);

        // Se pidió al repositorio agregar la orden una vez
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WhenProductStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        SetupCustomer(customerId, CreateCustomer(customerId));

        var producto = CreateProduct("Monitor", price: 1000m, stock: 2);
        SetupProducts(producto);

        var request = new OrderModel.OrderRequest(
            customerId,
            "Calle Falsa 123",
            "Calle Falsa 123",
            new List<OrderModel.OrderItemRequest> { new(producto.Id, 5) });

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateOrderAsync(request));

        // Assert
        Assert.Equal(ErrorMessages.InsufficientStock, exception.Message);
        Assert.Equal(2, producto.StockQuantity);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenCustomerDoesNotExist_ThrowsCustomerNotFoundException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        SetupCustomer(customerId, null); // el repositorio responde: "ese cliente no existe"

        var request = new OrderModel.OrderRequest(
            customerId,
            "Calle Falsa 123",
            "Calle Falsa 123",
            new List<OrderModel.OrderItemRequest> { new(Guid.NewGuid(), 1) });

        // Act
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateOrderAsync(request));

        // Assert
        Assert.Equal(ErrorMessages.CustomerNotFound, exception.Message);
        _repositoryMock.Verify(
            r => r.First<Product>(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<string[]>()),
            Times.Never);
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _repositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }
}

using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;

namespace Dsw2025Tpi.UnitTests;

public class ProductManagementServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly ProductManagementService _service;

    public ProductManagementServiceTests()
    {
        _service = new ProductManagementService(_repositoryMock.Object);
    }

    private static Product NewProduct(string sku, string name, string internalCode, bool isActive = true) =>
        new()
        {
            Sku = sku,
            Name = name,
            InternalCode = internalCode,
            Description = "Producto de prueba",
            CurrentUnitPrice = 100m,
            StockQuantity = 10,
            IsActive = isActive
        };

    // ---------------------------------------------------------------
    // 1) Búsqueda: ignora mayúsculas y filtra por nombre, SKU o código interno
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetProducts_WithSearchTerm_FiltersByNameSkuOrInternalCode()
    {
        // Arrange: "abc" aparece en el nombre, en el SKU y en el código interno de tres productos distintos
        var products = new List<Product>
        {
            NewProduct("SKU-1", "Laptop ABC", "INT-1"),
            NewProduct("xx-abc-1", "Mouse", "INT-2"),
            NewProduct("SKU-3", "Teclado", "Abc-9"),
            NewProduct("SKU-4", "Monitor", "INT-4")   // no coincide
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync("aBc", null, 1, 10);

        // Assert
        Assert.Equivalent(
            new[] { "SKU-1", "xx-abc-1", "SKU-3" },
            result.Items.Select(i => i.Sku));
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ---------------------------------------------------------------
    // 2) Paginación: página 2 de tamaño 5 sobre 15 productos
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetProducts_WithPaginationFilter_AppliesSkipAndTakeCorrectly()
    {
        // Arrange
        var products = Enumerable.Range(1, 15)
            .Select(i => NewProduct($"SKU-{i:00}", $"Producto {i:00}", $"INT-{i:00}"))
            .ToList();
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync(null, null, 2, 5);

        // Assert
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(15, result.Total);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ---------------------------------------------------------------
    // 3) Estado: sin incluir inactivos solo devuelve activos
    // ---------------------------------------------------------------
    [Fact]
    public async Task GetProducts_WhenIncludeInactiveIsFalse_ReturnsOnlyActiveProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            NewProduct("SKU-1", "Activo 1", "INT-1", isActive: true),
            NewProduct("SKU-2", "Activo 2", "INT-2", isActive: true),
            NewProduct("SKU-3", "Inactivo 1", "INT-3", isActive: false),
            NewProduct("SKU-4", "Inactivo 2", "INT-4", isActive: false)
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync(null, null, 1, 10);

        // Assert
        Assert.All(result.Items, item => Assert.True(item.IsActive));
        Assert.Equal(2, result.Items.Count());
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
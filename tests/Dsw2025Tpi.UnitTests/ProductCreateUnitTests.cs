using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Moq;

namespace Dsw2025Tpi.UnitTests;

public class ProductCreateUnitTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly ProductManagementService _service;

    public ProductCreateUnitTests()
    {
        _service = new ProductManagementService(_repositoryMock.Object);
    }

    // ---------------------------------------------------------------
    // 1) Datos válidos -> devuelve el producto y llama una vez a Add
    // ---------------------------------------------------------------
    [Fact]
    public async Task AddProduct_WithValidData_ReturnsProductAndCallsRepositoryAdd()
    {
        // Arrange
        var request = new ProductModel.ProductRequest(
            "SKU-001", "INT-001", "Notebook 14", "Notebook de prueba", 1200.50m, 10);

        Product? savedProduct = null;
        _repositoryMock
            .Setup(r => r.ExistsBySkuAsync(request.Sku, It.IsAny<Guid?>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => savedProduct = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Sku, result.Sku);
        Assert.Equal(request.InternalCode, result.InternalCode);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.CurrentUnitPrice, result.CurrentUnitPrice);
        Assert.Equal(request.StockQuantity, result.StockQuantity);
        Assert.True(result.IsActive);

        Assert.NotNull(savedProduct);
        Assert.Equal(result.Id, savedProduct!.Id);

        _repositoryMock.Verify(r => r.ExistsBySkuAsync(request.Sku, It.IsAny<Guid?>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // 2) SKU duplicado -> lanza excepción y NO agrega
    // ---------------------------------------------------------------
    [Fact]
    public async Task AddProduct_WithDuplicateSku_ThrowsDuplicatedEntityException()
    {
        // Arrange
        var request = new ProductModel.ProductRequest(
            "SKU-001", "INT-001", "Notebook 14", "Notebook de prueba", 1200.50m, 10);

        _repositoryMock
            .Setup(r => r.ExistsBySkuAsync(request.Sku, It.IsAny<Guid?>()))
            .ReturnsAsync(true);

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateProductAsync(request));

        // Assert
        Assert.Equal(ErrorMessages.SkuAlreadyExists, exception.Message);
        _repositoryMock.Verify(r => r.ExistsBySkuAsync(request.Sku, It.IsAny<Guid?>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    // ---------------------------------------------------------------
    // 3) Precio o stock inválido -> lanza excepción y no toca el repositorio
    // ---------------------------------------------------------------
    public static TheoryData<decimal, int> InvalidPriceOrStock => new()
    {
        { 0m, 10 },     // precio en cero
        { -5m, 10 },    // precio negativo
        { 100m, -1 },   // stock negativo
    };

    [Theory]
    [MemberData(nameof(InvalidPriceOrStock))]
    public async Task AddProduct_WithInvalidStockOrPrice_ThrowsInvalidDataException(decimal price, int stock)
    {
        // Arrange
        var request = new ProductModel.ProductRequest(
            "SKU-001", "INT-001", "Notebook 14", "Notebook de prueba", price, stock);

        // Act
        await Assert.ThrowsAsync<BusinessException>(() => _service.CreateProductAsync(request));

        // Assert: la validación corta antes de consultar o escribir en el repositorio
        _repositoryMock.Verify(r => r.ExistsBySkuAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }
}

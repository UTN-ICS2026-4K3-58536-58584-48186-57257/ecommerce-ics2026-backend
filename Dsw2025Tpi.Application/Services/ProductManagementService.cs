using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Application.Services;

public class ProductManagementService : IProductManagementService
{
    private readonly IProductRepository _repository;

    public ProductManagementService(IProductRepository repository)
    {
        _repository = repository;
    }

    // Crear productos
    public async Task<ProductModel.ProductResponse> CreateProductAsync(ProductModel.ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new BusinessException(ErrorMessages.SkuRequired);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException(ErrorMessages.NameRequired);

        if (request.CurrentUnitPrice <= 0)
            throw new BusinessException(ErrorMessages.PriceMustBePositive);

        if (request.StockQuantity < 0)
            throw new BusinessException(ErrorMessages.StockCannotBeNegative);

        var exists = await _repository.ExistsBySkuAsync(request.Sku);
        if (exists)
            throw new BusinessException(ErrorMessages.SkuAlreadyExists);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku,
            InternalCode = request.InternalCode,
            Name = request.Name,
            Description = request.Description,
            CurrentUnitPrice = request.CurrentUnitPrice,
            StockQuantity = request.StockQuantity,
            IsActive = true
        };

        await _repository.AddAsync(product);

        return ToResponse(product);
    }

    // Obtener todos los productos
    public async Task<PagedResult<ProductModel.ProductResponse>> GetAllProductsAsync(
        string? searchTerm,
        string? status,
        int pageNumber,
        int pageSize)
    {
        var (items, total) = await _repository.SearchAsync(searchTerm, status, pageNumber, pageSize);

        return new PagedResult<ProductModel.ProductResponse>
        {
            Total = total,
            Items = items.Select(ToResponse).ToList()
        };
    }

    // Obtener todos los productos admin
    public async Task<PagedResult<ProductModel.ProductResponse>> GetAllProductsAdminAsync(ProductModel.FilterProduct filter)
    {
        var (items, total) = await _repository.SearchAsync(
            filter.Search,
            filter.Status,
            filter.PageNumber ?? 1,
            filter.PageSize ?? 10);

        return new PagedResult<ProductModel.ProductResponse>
        {
            Total = total,
            Items = items.Select(ToResponse).ToList()
        };
    }

    // Obtener producto por ID
    public async Task<ProductModel.ProductResponse> GetProductByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null || !product.IsActive)
            throw new KeyNotFoundException(ErrorMessages.ProductNotFound);

        return ToResponse(product);
    }

    // Actualizar un producto
    public async Task<ProductModel.ProductResponse> UpdateProductAsync(Guid id, ProductModel.ProductRequest request)
    {
        var exists = await _repository.ExistsBySkuAsync(request.Sku, id);
        if (exists)
            throw new BusinessException(ErrorMessages.SkuAlreadyExists);

        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new BusinessException(ErrorMessages.SkuRequired);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException(ErrorMessages.NameRequired);

        if (request.CurrentUnitPrice <= 0)
            throw new BusinessException(ErrorMessages.PriceMustBePositive);

        if (request.StockQuantity < 0)
            throw new BusinessException(ErrorMessages.StockCannotBeNegative);

        var product = await _repository.GetByIdAsync(id);
        if (product == null || !product.IsActive)
            throw new KeyNotFoundException(ErrorMessages.ProductNotFound);

        product.Sku = request.Sku;
        product.InternalCode = request.InternalCode;
        product.Name = request.Name;
        product.Description = request.Description;
        product.CurrentUnitPrice = request.CurrentUnitPrice;
        product.StockQuantity = request.StockQuantity;

        await _repository.UpdateAsync(product);

        return ToResponse(product);
    }

    // Inhabilitar un producto
    public async Task DisableProductAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException(ErrorMessages.ProductNotFound);

        product.IsActive = false;
        await _repository.UpdateAsync(product);
    }

    private static ProductModel.ProductResponse ToResponse(Product product) =>
        new(
            product.Id,
            product.Sku,
            product.InternalCode,
            product.Name,
            product.Description,
            product.CurrentUnitPrice,
            product.StockQuantity,
            product.IsActive);
}

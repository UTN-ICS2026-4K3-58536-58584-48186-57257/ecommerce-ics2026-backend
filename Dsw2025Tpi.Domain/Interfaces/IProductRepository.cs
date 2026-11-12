using Dsw2025Tpi.Domain.Entities;

namespace Dsw2025Tpi.Domain.Interfaces;

public interface IProductRepository
{
    /// <summary>¿Existe un producto con ese SKU? Si se pasa excludingId, se ignora ese producto (útil al actualizar).</summary>
    Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId = null);

    Task<Product?> GetByIdAsync(Guid id);

    Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search, string? status, int pageNumber, int pageSize);

    /// <summary>Agrega el producto y persiste los cambios.</summary>
    Task AddAsync(Product product);

    /// <summary>Persiste los cambios de un producto ya cargado.</summary>
    Task UpdateAsync(Product product);
}

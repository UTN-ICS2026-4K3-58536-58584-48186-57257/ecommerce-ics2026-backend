using Dsw2025Tpi.Domain.Entities;
using Dsw2025Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dsw2025Tpi.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly Dsw2025TpiContext _context;

    public ProductRepository(Dsw2025TpiContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsBySkuAsync(string sku, Guid? excludingId = null)
    {
        return _context.Products.AnyAsync(p => p.Sku == sku && (excludingId == null || p.Id != excludingId));
    }

    public Task<Product?> GetByIdAsync(Guid id)
    {
        return _context.Products.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search, string? status, int pageNumber, int pageSize)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            bool isActive = status.ToLower() == "enabled";
            query = query.Where(p => p.IsActive == isActive);
        }

        var total = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        await _context.SaveChangesAsync();
    }
}

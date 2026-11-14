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

    public async Task<IReadOnlyList<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync();
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
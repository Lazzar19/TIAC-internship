using Microsoft.EntityFrameworkCore;
using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Infrastructure;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PageResult<Product>> GetAllAsync(ProductQueryParameters queryParameters)
        => QueryPageAsync(queryParameters);

    private async Task<PageResult<Product>> QueryPageAsync(ProductQueryParameters queryParameters)
    {
        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
            query = query.Where(p => p.Name.Contains(queryParameters.Search));

        if (queryParameters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= queryParameters.MinPrice.Value);

        if (queryParameters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= queryParameters.MaxPrice.Value);

        var count = await query.CountAsync();

        var items = await query.Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();

        return new PageResult<Product>
        {
            Items = items,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = count
        };
    }

    public Task<Product?> GetByIDAsync(int id)
        => _dbContext.Products.FirstOrDefaultAsync(product => product.ID == id);

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Product prod)
    {
        var stored = await _dbContext.Products.FirstOrDefaultAsync(product => product.ID == prod.ID);
        if (stored is null)
            return false;

        stored.Name = prod.Name;
        stored.Description = prod.Description;
        stored.Price = prod.Price;
        stored.RowVersion++;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        return true;
    }

    public async Task<DeleteProductResult> DeleteAsync(int id)
    {
        if (await _dbContext.UserProducts.AnyAsync(assignment => assignment.ProductID == id))
            return DeleteProductResult.Assigned;

        var product = await _dbContext.Products.FirstOrDefaultAsync(item => item.ID == id);
        if (product is null)
            return DeleteProductResult.NotFound;

        _dbContext.Products.Remove(product);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return DeleteProductResult.Assigned;
        }

        return DeleteProductResult.Deleted;
    }
}

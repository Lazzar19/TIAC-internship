using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure;
using WebAPI.Domain;
using WebAPI.Application;
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext dbContext_;
    
    public ProductRepository(ApplicationDbContext dbContext) { dbContext_ = dbContext; }

    public async Task<PageResult<Product>> GetAllAsync(ProductQuerryParametars queryParametars)
    {

        var query = dbContext_.Products.AsQueryable();
        
        if(!string.IsNullOrWhiteSpace(queryParametars.Search))
            query = query.Where(p => p.Name.Contains(queryParametars.Search));
        
        if(queryParametars.MinPrice.HasValue)
            query = query.Where(p => p.Price >= queryParametars.MinPrice.Value);
        
        if(queryParametars.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= queryParametars.MaxPrice.Value);
        
        var count =  await query.CountAsync();

        var items = await query.Skip((queryParametars.PageNumber - 1) * queryParametars.PageSize)
            .Take(queryParametars.PageSize).ToListAsync();

        return new PageResult<Product>
        {
            Items = items,
            PageNumber = queryParametars.PageNumber,
            PageSize = queryParametars.PageSize,
            TotalCount = count
        };


    }
    public async Task<Product?> GetByIDAsync(int id) => await dbContext_.Products.FindAsync(id);

    public async Task AddAsync(Product product)
    {
        await dbContext_.Products.AddAsync(product);
        await dbContext_.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product prod)
    {
        dbContext_.Products.Update(prod);
        await dbContext_.SaveChangesAsync();
    }

    public async  Task DeleteAsync(int id)
    {
        dbContext_.Products.Remove(await dbContext_.Products.FindAsync(id));
        await dbContext_.SaveChangesAsync();
    }
}
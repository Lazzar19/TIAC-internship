using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure;
using WebAPI.Domain;
using WebAPI.Application;
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext dbContext_;
    
    public ProductRepository(ApplicationDbContext dbContext) { dbContext_ = dbContext; }

    public async Task<IEnumerable<Product>> GetAllAsync() => await dbContext_.Products.ToListAsync();
    public async Task<Product?> GetByIDAsync(int id) => await dbContext_.Products.FindAsync(id);

    public async Task AddAsync(Product product)
    {
        await dbContext_.Products.AddAsync(product);
        await dbContext_.SaveChangesAsync();
    }

    public Task UpdateAsync(Product prod)
    {
        dbContext_.Products.Update(prod);
        
    }
    
    
}
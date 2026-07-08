using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure;

using WebAPI.Application;
using WebAPI.Domain;

public class UserProductRepository: IUserProductRepository
{
    private readonly ApplicationDbContext dbContext_;
    
    public UserProductRepository(ApplicationDbContext dbContext) { dbContext_ = dbContext; }


   

    public async Task<IEnumerable<UserProduct>> GetByUserIDAsync(int userId)
        => await dbContext_.UserProducts
            .Include(up => up.Product)          // učitava i Product podatke, potrebno za ToDto()
            .Where(up => up.UserID == userId)
            .ToListAsync();

    public async Task<UserProduct?> GetAsync(int userID, int productID) => 
        await dbContext_.UserProducts.Include(up => up.Product)
            .FirstOrDefaultAsync(up => up.UserID == userID && up.ProductID == productID);


    public async Task AddOrUpdateAsync(UserProduct userProduct)
    {
      var existing = await dbContext_.UserProducts.
          FirstOrDefaultAsync(up => up.UserID == userProduct.UserID && up.ProductID == userProduct.ProductID);

      if (existing is null)
      {
          await dbContext_.UserProducts.AddAsync(userProduct);
      }
      else
      {
         existing.NumberOfProducts = userProduct.NumberOfProducts;
      }
      
      await dbContext_.SaveChangesAsync();
      
    }

    public async  Task DeleteAsync(UserProduct userProduct)
    {
        dbContext_.UserProducts.Remove(userProduct);
        await dbContext_.SaveChangesAsync();
    }
    
    
}
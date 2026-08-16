using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure;

using WebAPI.Application;
using WebAPI.Domain;

public class UserProductRepository: IUserProductRepository
{
    private readonly ApplicationDbContext dbContext_;
    private readonly IProductRepository _productRepository;
    public UserProductRepository(ApplicationDbContext dbContext, IProductRepository productRepository)
    {
        dbContext_ = dbContext;
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<UserProduct>> GetByUserIDAsync(int userId)
        => await dbContext_.UserProducts
            .Include(up => up.Product)          
            .Where(up => up.UserID == userId)
            .ToListAsync();

    public async Task<UserProduct?> GetAsync(int userID, int productID) => 
        await dbContext_.UserProducts.Include(up => up.Product)
            .FirstOrDefaultAsync(up => up.UserID == userID && up.ProductID == productID);


    public async Task<AssignProductResult> AssignAsync(int userId, int productId, int quantity)
    {
        var product = await dbContext_.Products.FindAsync(productId);
        if (product is null)
            return AssignProductResult.ProductNotFound;

        var existing = await dbContext_.UserProducts
            .FirstOrDefaultAsync(up => up.UserID == userId && up.ProductID == productId);

        var previousQuantity = existing?.NumberOfProducts ?? 0;
        var delta = quantity - previousQuantity;

        if (delta > 0 && product.Stock < delta)
            return AssignProductResult.InsufficientStock;

        product.Stock -= delta;

        if (existing is null)
        {
            await dbContext_.UserProducts.AddAsync(new UserProduct
            {
                UserID = userId,
                ProductID = productId,
                NumberOfProducts = quantity
            });
            
        }
        else
        {
            existing.NumberOfProducts = quantity;
        }

        await dbContext_.SaveChangesAsync();
        await _productRepository.InvalidateCacheAsync(productId);
        return  AssignProductResult.Success;

    }

    

    public async  Task DeleteAsync(UserProduct userProduct)
    {
        var product = await dbContext_.Products.FindAsync(userProduct.ProductID);
        if (product != null)
        {
            product.Stock += userProduct.NumberOfProducts;
        }

        dbContext_.UserProducts.Remove(userProduct);
        await dbContext_.SaveChangesAsync();
        await  _productRepository.InvalidateCacheAsync(userProduct.ProductID);
    }
    
    
}
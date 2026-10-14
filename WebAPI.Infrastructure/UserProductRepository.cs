using Microsoft.EntityFrameworkCore;

namespace WebAPI.Infrastructure;

using WebAPI.Application;
using WebAPI.Domain;

public class UserProductRepository: IUserProductRepository
{
    private readonly ApplicationDbContext dbContext_;

    public UserProductRepository(ApplicationDbContext dbContext)
    {
        dbContext_ = dbContext;
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
        if (!await dbContext_.Users.AnyAsync(user => user.ID == userId))
            return AssignProductResult.UserNotFound;

        if (!await dbContext_.Products.AnyAsync(product => product.ID == productId))
            return AssignProductResult.ProductNotFound;

        if (dbContext_.Database.IsRelational())
            return await AssignRelationalAsync(userId, productId, quantity);

        return await AssignTrackedAsync(userId, productId, quantity);
    }

    public async Task<bool> DeleteAsync(UserProduct userProduct)
    {
        if (dbContext_.Database.IsRelational())
            return await DeleteRelationalAsync(userProduct);

        var product = await dbContext_.Products.FindAsync(userProduct.ProductID);
        if (product != null)
        {
            product.Stock += userProduct.NumberOfProducts;
            product.RowVersion++;
        }

        dbContext_.UserProducts.Remove(userProduct);
        await dbContext_.SaveChangesAsync();
        return true;
    }

    private async Task<AssignProductResult> AssignRelationalAsync(int userId, int productId, int quantity)
    {
        await using var transaction = await dbContext_.Database.BeginTransactionAsync();

        var existing = await dbContext_.UserProducts
            .FirstOrDefaultAsync(up => up.UserID == userId && up.ProductID == productId);
        var previousQuantity = existing?.NumberOfProducts ?? 0;
        var delta = quantity - previousQuantity;

        if (delta > 0)
        {
            var updated = await AdjustStockAsync(productId, -delta, requireAvailable: delta);
            if (updated == 0)
            {
                await transaction.RollbackAsync();
                var stillExists = await dbContext_.Products.AnyAsync(product => product.ID == productId);
                return stillExists
                    ? AssignProductResult.InsufficientStock
                    : AssignProductResult.ProductNotFound;
            }
        }
        else if (delta < 0)
        {
            await AdjustStockAsync(productId, -delta, requireAvailable: null);
        }

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

        try
        {
            await dbContext_.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return AssignProductResult.Conflict;
        }

        return AssignProductResult.Success;
    }

    private async Task<AssignProductResult> AssignTrackedAsync(int userId, int productId, int quantity)
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
        product.RowVersion++;

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

        try
        {
            await dbContext_.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssignProductResult.Conflict;
        }

        return AssignProductResult.Success;
    }

    private async Task<bool> DeleteRelationalAsync(UserProduct userProduct)
    {
        await using var transaction = await dbContext_.Database.BeginTransactionAsync();

        var deleted = await dbContext_.UserProducts
            .Where(up => up.UserID == userProduct.UserID && up.ProductID == userProduct.ProductID)
            .ExecuteDeleteAsync();
        if (deleted == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await AdjustStockAsync(userProduct.ProductID, userProduct.NumberOfProducts, requireAvailable: null);
        await transaction.CommitAsync();
        return true;
    }

    private Task<int> AdjustStockAsync(int productId, int stockDelta, int? requireAvailable)
    {
        var products = dbContext_.Products.Where(product => product.ID == productId);
        if (requireAvailable.HasValue)
            products = products.Where(product => product.Stock >= requireAvailable.Value);

        return products.ExecuteUpdateAsync(setters => setters
            .SetProperty(product => product.Stock, product => product.Stock + stockDelta)
            .SetProperty(product => product.RowVersion, product => product.RowVersion + 1));
    }
}

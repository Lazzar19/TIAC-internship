using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Validators;

namespace WebAPI.Infrastructure;

using WebAPI.Application;
using WebAPI.Domain;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext dbContext_;
    private readonly IProductRepository productRepository_;
    
    public UserRepository(ApplicationDbContext dbContext, IProductRepository productRepository)
    {
        dbContext_ = dbContext;
        productRepository_ = productRepository;
    }


    public async Task<IEnumerable<User>> GetAllAsync() => await dbContext_.Users.ToListAsync();
    public async Task<User?> GetByIDAsync(int userID) =>  await dbContext_.Users.FindAsync(userID);
    public async Task<User?> GetByEmailAsync(string email) => 
        await dbContext_.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        await dbContext_.Users.AddAsync(user);
        await dbContext_.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        dbContext_.Users.Update(user);
        await dbContext_.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        if (dbContext_.Database.IsRelational())
        {
            await using var transaction = await dbContext_.Database.BeginTransactionAsync();
            var productIds = await RestoreStockAndDeleteAsync(user);
            await transaction.CommitAsync();
            await InvalidateProductsAsync(productIds);
            return;
        }

        var restoredProductIds = await RestoreStockAndDeleteAsync(user);
        await InvalidateProductsAsync(restoredProductIds);
    }

    private async Task<List<int>> RestoreStockAndDeleteAsync(User user)
    {
        var assignments = await dbContext_.UserProducts
            .Where(up => up.UserID == user.ID)
            .ToListAsync();
        var productIds = assignments.Select(assignment => assignment.ProductID).Distinct().ToList();

        var products = productIds.Count == 0
            ? new Dictionary<int, Product>()
            : await dbContext_.Products
                .Where(product => productIds.Contains(product.ID))
                .ToDictionaryAsync(product => product.ID);

        var tokens = await dbContext_.RefreshTokens
            .Where(token => token.UserId == user.ID)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            if (products.TryGetValue(assignment.ProductID, out var product))
                product.Stock += assignment.NumberOfProducts;
        }

        dbContext_.UserProducts.RemoveRange(assignments);
        dbContext_.RefreshTokens.RemoveRange(tokens);
        dbContext_.Users.Remove(user);
        await dbContext_.SaveChangesAsync();
        return productIds;
    }

    private async Task InvalidateProductsAsync(IEnumerable<int> productIds)
    {
        foreach (var productId in productIds)
            await productRepository_.InvalidateCacheAsync(productId);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext_.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }
}
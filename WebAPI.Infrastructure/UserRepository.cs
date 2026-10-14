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


    public async Task<PageResult<User>> GetAllAsync(int pageNumber, int pageSize)
    {
        pageNumber = PageLimits.PageNumber(pageNumber);
        pageSize = PageLimits.PageSize(pageSize);

        var query = dbContext_.Users.OrderBy(user => user.ID);
        var count = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PageResult<User>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = count
        };
    }
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
            if (dbContext_.Database.IsRelational())
            {
                await dbContext_.Products
                    .Where(product => product.ID == assignment.ProductID)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.Stock, product => product.Stock + assignment.NumberOfProducts)
                        .SetProperty(product => product.RowVersion, product => product.RowVersion + 1));
            }
            else if (products.TryGetValue(assignment.ProductID, out var product))
            {
                product.Stock += assignment.NumberOfProducts;
                product.RowVersion++;
            }
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
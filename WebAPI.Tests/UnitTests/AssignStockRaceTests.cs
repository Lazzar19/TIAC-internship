using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebAPI.Application;
using WebAPI.Domain;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.UnitTests;

public class AssignStockRaceTests
{
    [Fact]
    public async Task Parallel_Assign_Allows_Only_One_Sale_When_Stock_Is_One()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stock-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        try
        {
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Users.AddRange(
                    new User { Username = "a", Email = "a@test.com", PasswordHash = "x" },
                    new User { Username = "b", Email = "b@test.com", PasswordHash = "x" });
                setup.Products.Add(new Product
                {
                    Name = "last unit",
                    Description = "one left",
                    Price = 1,
                    Stock = 1,
                    CreatedAt = DateTime.UtcNow
                });
                await setup.SaveChangesAsync();
            }

            var results = await Task.WhenAll(AssignAsync(options, 1), AssignAsync(options, 2));

            results.Count(result => result == AssignProductResult.Success).Should().Be(1);
            results.Count(result => result == AssignProductResult.InsufficientStock).Should().Be(1);

            await using var check = new ApplicationDbContext(options);
            var product = await check.Products.SingleAsync();
            product.Stock.Should().Be(0);
            (await check.UserProducts.CountAsync()).Should().Be(1);
        }
        finally
        {
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }
    }

    private static async Task<AssignProductResult> AssignAsync(DbContextOptions<ApplicationDbContext> options, int userId)
    {
        await using var db = new ApplicationDbContext(options);
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()), NullLoggerFactory.Instance);
        var repository = new UserProductRepository(db, new ProductRepository(db, cache));
        return await repository.AssignAsync(userId, productId: 1, quantity: 1);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

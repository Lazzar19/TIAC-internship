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

public class ProductCacheTests
{
    [Fact]
    public async Task GetAll_Sees_Name_And_Stock_Changes_Immediately()
    {
        await using var context = CreateContext();
        var cache = CreateCache();
        var products = new ProductRepository(context, cache, NullLogger<ProductRepository>.Instance);
        var assignments = new UserProductRepository(context, products);

        var product = new Product
        {
            Name = "Desk",
            Description = "wood",
            Price = 10,
            Stock = 20,
            CreatedAt = DateTime.UtcNow
        };
        await products.AddAsync(product);
        context.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
        await context.SaveChangesAsync();
        var userId = (await context.Users.SingleAsync()).ID;

        var before = await products.GetAllAsync(new ProductQuerryParametars());
        before.Items.Should().ContainSingle().Which.Name.Should().Be("Desk");

        var updated = await products.UpdateAsync(new Product
        {
            ID = product.ID,
            Name = "Table",
            Description = "wood",
            Price = 10
        });
        updated.Should().BeTrue();

        var assigned = await assignments.AssignAsync(userId, product.ID, 5);
        assigned.Should().Be(AssignProductResult.Success);

        var after = await products.GetAllAsync(new ProductQuerryParametars());
        var item = after.Items.Should().ContainSingle().Subject;
        item.Name.Should().Be("Table");
        item.Stock.Should().Be(15);
    }

    [Fact]
    public async Task GetAll_Reads_Database_When_Cache_Throws()
    {
        await using var context = CreateContext();
        context.Products.Add(new Product
        {
            Name = "Lamp",
            Description = "desk",
            Price = 12,
            Stock = 3,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = new ProductRepository(context, new ThrowingDistributedCache(), NullLogger<ProductRepository>.Instance);
        var page = await repository.GetAllAsync(new ProductQuerryParametars());

        page.Items.Should().ContainSingle().Which.Name.Should().Be("Lamp");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IDistributedCache CreateCache()
        => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()), NullLoggerFactory.Instance);

    private sealed class ThrowingDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new InvalidOperationException("cache unavailable");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => throw new InvalidOperationException("cache unavailable");

        public void Refresh(string key) => throw new InvalidOperationException("cache unavailable");

        public Task RefreshAsync(string key, CancellationToken token = default)
            => throw new InvalidOperationException("cache unavailable");

        public void Remove(string key) => throw new InvalidOperationException("cache unavailable");

        public Task RemoveAsync(string key, CancellationToken token = default)
            => throw new InvalidOperationException("cache unavailable");

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => throw new InvalidOperationException("cache unavailable");

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            => throw new InvalidOperationException("cache unavailable");
    }
}

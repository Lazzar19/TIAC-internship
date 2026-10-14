using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using WebAPI.Application;
using WebAPI.Domain;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.Integration_Tests;

public class PostgresContainerTests
{
    [DockerFact]
    public async Task Migration_Rejects_A_Duplicate_Email_And_A_Second_Sale_Of_The_Last_Unit()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
            await setup.SaveChangesAsync();

            setup.Users.Add(new User { Username = "ana-2", Email = "ana@mail.com", PasswordHash = "x" });
            var saveDuplicate = async () => await setup.SaveChangesAsync();
            var error = await saveDuplicate.Should().ThrowAsync<DbUpdateException>();
            UniqueConstraintViolation.Is(error.Which).Should().BeTrue();
        }

        await using (var setup = new ApplicationDbContext(options))
        {
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

        await using var ids = new ApplicationDbContext(options);
        var users = await ids.Users.Where(user => user.Email == "a@test.com" || user.Email == "b@test.com").ToListAsync();
        var productId = (await ids.Products.SingleAsync()).ID;

        var results = await Task.WhenAll(
            AssignAsync(options, users[0].ID, productId),
            AssignAsync(options, users[1].ID, productId));

        results.Count(result => result == AssignProductResult.Success).Should().Be(1);
        results.Count(result => result == AssignProductResult.InsufficientStock).Should().Be(1);

        await using var check = new ApplicationDbContext(options);
        (await check.Products.SingleAsync()).Stock.Should().Be(0);
        (await check.UserProducts.CountAsync()).Should().Be(1);
    }

    private static async Task<AssignProductResult> AssignAsync(DbContextOptions<ApplicationDbContext> options, int userId, int productId)
    {
        await using var db = new ApplicationDbContext(options);
        var repository = new UserProductRepository(db);
        return await repository.AssignAsync(userId, productId, 1);
    }
}

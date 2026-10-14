using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebAPI.Domain;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.UnitTests;

public class SqliteMigrationTests
{
    [Fact]
    public async Task Migrate_Creates_The_Production_Schema_On_Sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"migrate-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path}")
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();

            var applied = await db.Database.GetAppliedMigrationsAsync();
            applied.Should().NotBeEmpty();

            db.Products.Add(new Product
            {
                Name = "Lamp",
                Description = "desk",
                Price = 12,
                Stock = 3,
                CreatedAt = DateTime.UtcNow
            });
            db.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
            await db.SaveChangesAsync();

            var product = await db.Products.SingleAsync();
            product.ID.Should().BeGreaterThan(0);
            product.RowVersion.Should().Be(0);

            var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('RefreshTokens')").ToListAsync();
            columns.Should().Contain("SessionStartedAt");
            columns.Should().Contain("ReplacedByTokenHash");
        }
        finally
        {
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

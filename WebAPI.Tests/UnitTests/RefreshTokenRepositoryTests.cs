using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.UnitTests;

public class RefreshTokenRepositoryTests
{
    [Fact]
    public async Task Parallel_Refresh_Of_The_Same_Token_Succeeds_Once()
    {
        var path = Path.Combine(Path.GetTempPath(), $"refresh-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var rawToken = "shared-refresh-token";
        var now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

        try
        {
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
                await setup.SaveChangesAsync();
                var userId = (await setup.Users.SingleAsync()).ID;
                setup.RefreshTokens.Add(new RefreshToken
                {
                    Token = RefreshTokenHasher.Hash(rawToken),
                    UserId = userId,
                    ExpiresAt = now.AddDays(1),
                    SessionStartedAt = now,
                    CreatedAt = now
                });
                await setup.SaveChangesAsync();
            }

            var results = await Task.WhenAll(
                RotateAsync(options, rawToken, "next-a", now),
                RotateAsync(options, rawToken, "next-b", now));

            results.Count(result => result.Succeeded).Should().Be(1);
            results.Count(result => !result.Succeeded).Should().Be(1);
        }
        finally
        {
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }
    }

    [Fact]
    public async Task Refresh_Is_Rejected_When_The_Session_Is_Older_Than_Seven_Days()
    {
        await using var context = CreateContext();
        var now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        context.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
        await context.SaveChangesAsync();
        var userId = (await context.Users.SingleAsync()).ID;
        context.RefreshTokens.Add(new RefreshToken
        {
            Token = RefreshTokenHasher.Hash("old-session"),
            UserId = userId,
            ExpiresAt = now.AddHours(1),
            SessionStartedAt = now.AddDays(-7),
            CreatedAt = now.AddDays(-7)
        });
        await context.SaveChangesAsync();

        var result = await new RefreshTokenRepository(context).RotateAsync("old-session", "replacement", now);

        result.Succeeded.Should().BeFalse();
        (await context.RefreshTokens.CountAsync(token => token.RevokedAt == null)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteExpired_Removes_Only_Revoked_Tokens()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User { Username = "ana", Email = "ana@mail.com", PasswordHash = "x" });
        await context.SaveChangesAsync();
        var userId = (await context.Users.SingleAsync()).ID;
        context.RefreshTokens.AddRange(
            new RefreshToken
            {
                Token = RefreshTokenHasher.Hash("gone"),
                UserId = userId,
                ExpiresAt = now.AddMinutes(-5),
                RevokedAt = now.AddMinutes(-10),
                SessionStartedAt = now.AddDays(-1)
            },
            new RefreshToken
            {
                Token = RefreshTokenHasher.Hash("expired-but-active"),
                UserId = userId,
                ExpiresAt = now.AddMinutes(-5),
                SessionStartedAt = now.AddDays(-1)
            },
            new RefreshToken
            {
                Token = RefreshTokenHasher.Hash("fresh"),
                UserId = userId,
                ExpiresAt = now.AddDays(1),
                SessionStartedAt = now
            });
        await context.SaveChangesAsync();

        await new RefreshTokenRepository(context).DeleteExpiredAsync();

        var remaining = await context.RefreshTokens.Select(token => token.Token).ToListAsync();
        remaining.Should().BeEquivalentTo(
            RefreshTokenHasher.Hash("expired-but-active"),
            RefreshTokenHasher.Hash("fresh"));
    }

    private static async Task<Application.Interfaces.RefreshRotationResult> RotateAsync(
        DbContextOptions<ApplicationDbContext> options,
        string rawToken,
        string newRawToken,
        DateTime utcNow)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000;";
        await command.ExecuteNonQueryAsync();
        return await new RefreshTokenRepository(db).RotateAsync(rawToken, newRawToken, utcNow);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

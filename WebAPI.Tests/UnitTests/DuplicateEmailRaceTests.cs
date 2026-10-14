using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;
using WebAPI.Infrastructure;

namespace WebAPI.Tests.UnitTests;

public class DuplicateEmailRaceTests
{
    [Fact]
    public async Task Parallel_Register_With_The_Same_Email_Rejects_One_As_A_Unique_Violation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"email-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        try
        {
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            var tasks = new[]
            {
                InsertAsync(options),
                InsertAsync(options)
            };

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
            }

            tasks.Count(task => task.Status == TaskStatus.RanToCompletion).Should().Be(1);
            var failed = tasks.Single(task => task.IsFaulted);
            var error = failed.Exception!.InnerExceptions.Should().ContainSingle().Subject.Should().BeOfType<EmailAlreadyExistsException>().Subject;
            UniqueConstraintViolation.Is((DbUpdateException)error.InnerException!).Should().BeTrue();
        }
        finally
        {
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }
    }

    private static async Task InsertAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000;";
        await command.ExecuteNonQueryAsync();

        var repository = new UserRepository(db);
        await repository.AddAsync(new User
        {
            Username = "ana",
            Email = "ana@mail.com",
            PasswordHash = "x"
        });
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}

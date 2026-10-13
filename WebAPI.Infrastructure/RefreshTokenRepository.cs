using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Interfaces;
using WebAPI.Domain;

namespace WebAPI.Infrastructure;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private static readonly TimeSpan AbsoluteSessionLifetime = TimeSpan.FromDays(7);

    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        refreshToken.Token = RefreshTokenHasher.Hash(refreshToken.Token);
        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token) => await _dbContext.RefreshTokens
        .Include(rt => rt.User)
        .FirstOrDefaultAsync(rt => rt.Token == RefreshTokenHasher.Hash(token));

    public async Task<RefreshRotationResult> RotateAsync(string rawToken, string newRawToken, DateTime utcNow)
    {
        if (_dbContext.Database.IsRelational())
            return await RotateRelationalAsync(rawToken, newRawToken, utcNow);

        return await RotateTrackedAsync(rawToken, newRawToken, utcNow);
    }

    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(int userId)
    {
        return await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();
    }

    public async Task RevokeAllByUserIdAsync(int userId)
    {
        await RevokeActiveAsync(userId, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        var expiredTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.RevokedAt != null && rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _dbContext.RefreshTokens.RemoveRange(expiredTokens);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<RefreshRotationResult> RotateRelationalAsync(string rawToken, string newRawToken, DateTime utcNow)
    {
        var hash = RefreshTokenHasher.Hash(rawToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var claimed = await _dbContext.RefreshTokens
            .Where(token => token.Token == hash && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, utcNow));

        var stored = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == hash);

        if (stored is null)
        {
            await transaction.RollbackAsync();
            return RefreshRotationResult.Failed();
        }

        if (claimed == 0)
        {
            await RevokeActiveAsync(stored.UserId, utcNow);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return RefreshRotationResult.Failed();
        }

        if (stored.ExpiresAt <= utcNow || SessionExpired(stored, utcNow))
        {
            await transaction.CommitAsync();
            return RefreshRotationResult.Failed();
        }

        var successor = CreateSuccessor(stored, newRawToken, utcNow);
        stored.ReplacedByTokenHash = successor.Token;
        _dbContext.RefreshTokens.Add(successor);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return RefreshRotationResult.Success(stored.User);
    }

    private async Task<RefreshRotationResult> RotateTrackedAsync(string rawToken, string newRawToken, DateTime utcNow)
    {
        var stored = await GetByTokenAsync(rawToken);
        if (stored is null)
            return RefreshRotationResult.Failed();

        if (stored.RevokedAt is not null)
        {
            await RevokeAllByUserIdAsync(stored.UserId);
            return RefreshRotationResult.Failed();
        }

        if (stored.ExpiresAt <= utcNow || SessionExpired(stored, utcNow))
        {
            stored.RevokedAt = utcNow;
            await _dbContext.SaveChangesAsync();
            return RefreshRotationResult.Failed();
        }

        stored.RevokedAt = utcNow;
        var successor = CreateSuccessor(stored, newRawToken, utcNow);
        stored.ReplacedByTokenHash = successor.Token;
        _dbContext.RefreshTokens.Add(successor);
        await _dbContext.SaveChangesAsync();
        return RefreshRotationResult.Success(stored.User);
    }

    private async Task RevokeActiveAsync(int userId, DateTime utcNow)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
            token.RevokedAt = utcNow;
    }

    private static RefreshToken CreateSuccessor(RefreshToken current, string newRawToken, DateTime utcNow)
    {
        return new RefreshToken
        {
            Token = RefreshTokenHasher.Hash(newRawToken),
            UserId = current.UserId,
            ExpiresAt = utcNow.AddDays(1),
            SessionStartedAt = current.SessionStartedAt,
            CreatedAt = utcNow
        };
    }

    private static bool SessionExpired(RefreshToken token, DateTime utcNow)
        => utcNow - token.SessionStartedAt >= AbsoluteSessionLifetime;
}

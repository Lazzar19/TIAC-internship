using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;

namespace WebAPI.Infrastructure;

using WebAPI.Application.Interfaces;

public class RefreshTokenRepository : IRefreshTokenRepository
{

    private readonly ApplicationDbContext _dbContext;
    
    public RefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token) => await _dbContext.RefreshTokens
        .Include(rt => rt.User)
        .FirstOrDefaultAsync(rt => rt.Token == token);

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
        var tokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        _dbContext.RefreshTokens.UpdateRange(tokens);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteExpiredAsync()
    {
        var expiredTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _dbContext.RefreshTokens.RemoveRange(expiredTokens);
        await _dbContext.SaveChangesAsync();
    }
}
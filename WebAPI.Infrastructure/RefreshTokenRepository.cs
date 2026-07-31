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

    public async  Task<RefreshToken?> GetByTokenAsync(string token) => await _dbContext.RefreshTokens
        .Include(rt => rt.User)
        .FirstOrDefaultAsync(rt => rt.Token == token);

    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.isRevoked = true;
        await _dbContext.SaveChangesAsync();
    }
    
    
}
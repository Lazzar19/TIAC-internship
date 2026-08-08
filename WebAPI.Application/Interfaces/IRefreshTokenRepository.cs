using WebAPI.Domain;

namespace WebAPI.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(RefreshToken refreshToken);
    Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(int userId);
    Task RevokeAllByUserIdAsync(int userId);
    Task DeleteExpiredAsync();
    
}
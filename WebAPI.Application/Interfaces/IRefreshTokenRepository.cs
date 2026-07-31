using WebAPI.Domain;

namespace WebAPI.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task RevokeAsync(RefreshToken refreshToken);
    
}
using WebAPI.Domain;

namespace WebAPI.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshRotationResult> RotateAsync(string rawToken, string newRawToken, DateTime utcNow);
    Task RevokeAsync(RefreshToken refreshToken);
    Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(int userId);
    Task RevokeAllByUserIdAsync(int userId);
    Task DeleteExpiredAsync();
}

public sealed class RefreshRotationResult
{
    public bool Succeeded { get; init; }
    public User? User { get; init; }

    public static RefreshRotationResult Success(User user) => new() { Succeeded = true, User = user };

    public static RefreshRotationResult Failed() => new();
}
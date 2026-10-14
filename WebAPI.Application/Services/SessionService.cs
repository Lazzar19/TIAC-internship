using WebAPI.Application.Interfaces;

namespace WebAPI.Application;

public sealed class SessionService
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public SessionService(IRefreshTokenRepository refreshTokens)
    {
        _refreshTokens = refreshTokens;
    }

    public Task RevokeAsync(int userId)
        => _refreshTokens.RevokeAllByUserIdAsync(userId);
}

using WebAPI.Application.Interfaces;
using WebAPI.Domain;

namespace WebAPI.Application;

public sealed class UserAccountService
{
    private readonly IUserRepository _users;
    private readonly IProductCache _cache;

    public UserAccountService(IUserRepository users, IProductCache cache)
    {
        _users = users;
        _cache = cache;
    }

    public async Task DeleteAsync(User user)
    {
        var productIds = await _users.DeleteAsync(user);
        foreach (var productId in productIds)
            await _cache.InvalidateAsync(productId);
    }
}

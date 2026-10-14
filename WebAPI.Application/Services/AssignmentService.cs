using WebAPI.Domain;

namespace WebAPI.Application;

public sealed class AssignmentService
{
    private readonly IUserProductRepository _assignments;
    private readonly IProductCache _cache;

    public AssignmentService(IUserProductRepository assignments, IProductCache cache)
    {
        _assignments = assignments;
        _cache = cache;
    }

    public Task<IEnumerable<UserProduct?>> GetByUserIdAsync(int userId)
        => _assignments.GetByUserIDAsync(userId);

    public Task<UserProduct?> GetAsync(int userId, int productId)
        => _assignments.GetAsync(userId, productId);

    public async Task<AssignProductResult> AssignAsync(int userId, int productId, int quantity)
    {
        var result = await _assignments.AssignAsync(userId, productId, quantity);
        if (result == AssignProductResult.Success)
            await _cache.InvalidateAsync(productId);
        return result;
    }

    public async Task<bool> UnassignAsync(UserProduct assignment)
    {
        var deleted = await _assignments.DeleteAsync(assignment);
        if (deleted)
            await _cache.InvalidateAsync(assignment.ProductID);
        return deleted;
    }
}

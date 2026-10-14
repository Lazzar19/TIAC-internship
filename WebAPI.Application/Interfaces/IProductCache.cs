using WebAPI.Domain;

namespace WebAPI.Application;

public interface IProductCache
{
    Task<CacheRead<PageResult<Product>>> TryGetListAsync(ProductQueryParameters queryParameters);
    Task TrySetListAsync(ProductQueryParameters queryParameters, PageResult<Product> page, string version);
    Task<CacheRead<Product>> TryGetItemAsync(int id);
    Task TrySetItemAsync(Product product);
    Task InvalidateListsAsync();
    Task InvalidateAsync(int productId);
}

public enum CacheReadStatus
{
    Hit,
    Miss,
    Unavailable
}

public readonly record struct CacheRead<T>(CacheReadStatus Status, T? Value, string? Version = null);

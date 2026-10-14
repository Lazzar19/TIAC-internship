using WebAPI.Domain;

namespace WebAPI.Application;

public sealed class ProductService
{
    private readonly IProductRepository _products;
    private readonly IProductCache _cache;

    public ProductService(IProductRepository products, IProductCache cache)
    {
        _products = products;
        _cache = cache;
    }

    public async Task<PageResult<Product>> GetAllAsync(ProductQueryParameters queryParameters)
    {
        var cached = await _cache.TryGetListAsync(queryParameters);
        if (cached.Status == CacheReadStatus.Hit && cached.Value is not null)
            return cached.Value;

        var result = await _products.GetAllAsync(queryParameters);
        if (cached.Status == CacheReadStatus.Miss && cached.Version is not null)
            await _cache.TrySetListAsync(queryParameters, result, cached.Version);

        return result;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var cached = await _cache.TryGetItemAsync(id);
        if (cached.Status == CacheReadStatus.Hit)
            return cached.Value;

        var product = await _products.GetByIDAsync(id);
        if (product is not null && cached.Status == CacheReadStatus.Miss)
            await _cache.TrySetItemAsync(product);

        return product;
    }

    public async Task AddAsync(Product product)
    {
        await _products.AddAsync(product);
        await _cache.TrySetItemAsync(product);
        await _cache.InvalidateListsAsync();
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        var updated = await _products.UpdateAsync(product);
        if (!updated)
            return false;

        var stored = await _products.GetByIDAsync(product.ID);
        if (stored is not null)
            await _cache.TrySetItemAsync(stored);
        await _cache.InvalidateListsAsync();
        return true;
    }

    public async Task<DeleteProductResult> DeleteAsync(int id)
    {
        var result = await _products.DeleteAsync(id);
        if (result == DeleteProductResult.Deleted)
            await _cache.InvalidateAsync(id);
        return result;
    }
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Infrastructure;

public class ProductCache : IProductCache
{
    private const string ListVersionKey = "products:list:version";

    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly DistributedCacheEntryOptions ListCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    private static readonly DistributedCacheEntryOptions ItemCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductCache> _logger;

    public ProductCache(IDistributedCache cache, ILogger<ProductCache>? logger = null)
    {
        _cache = cache;
        _logger = logger ?? NullLogger<ProductCache>.Instance;
    }

    public async Task<CacheRead<PageResult<Product>>> TryGetListAsync(ProductQueryParameters queryParameters)
    {
        var versionRead = await TryGetStringAsync(ListVersionKey);
        if (!versionRead.Ok)
            return new CacheRead<PageResult<Product>>(CacheReadStatus.Unavailable, null);

        var version = string.IsNullOrEmpty(versionRead.Value) ? "1" : versionRead.Value;
        var cachedResult = await TryGetStringAsync(BuildListCacheKey(version, queryParameters));
        if (!cachedResult.Ok)
            return new CacheRead<PageResult<Product>>(CacheReadStatus.Unavailable, null);

        if (string.IsNullOrWhiteSpace(cachedResult.Value))
            return new CacheRead<PageResult<Product>>(CacheReadStatus.Miss, null, version);

        var cachedPage = JsonSerializer.Deserialize<PageResult<Product>>(cachedResult.Value, CacheJsonOptions);
        return cachedPage is null
            ? new CacheRead<PageResult<Product>>(CacheReadStatus.Miss, null, version)
            : new CacheRead<PageResult<Product>>(CacheReadStatus.Hit, cachedPage, version);
    }

    public async Task TrySetListAsync(ProductQueryParameters queryParameters, PageResult<Product> page, string version)
    {
        await TrySetStringAsync(
            BuildListCacheKey(version, queryParameters),
            JsonSerializer.Serialize(page, CacheJsonOptions),
            ListCacheOptions);
    }

    public async Task<CacheRead<Product>> TryGetItemAsync(int id)
    {
        var cachedProduct = await TryGetStringAsync(BuildItemCacheKey(id));
        if (!cachedProduct.Ok)
            return new CacheRead<Product>(CacheReadStatus.Unavailable, null);

        if (string.IsNullOrWhiteSpace(cachedProduct.Value))
            return new CacheRead<Product>(CacheReadStatus.Miss, null);

        var product = JsonSerializer.Deserialize<Product>(cachedProduct.Value, CacheJsonOptions);
        return product is null
            ? new CacheRead<Product>(CacheReadStatus.Miss, null)
            : new CacheRead<Product>(CacheReadStatus.Hit, product);
    }

    public Task TrySetItemAsync(Product product)
        => TrySetStringAsync(
            BuildItemCacheKey(product.ID),
            JsonSerializer.Serialize(product, CacheJsonOptions),
            ItemCacheOptions);

    public Task InvalidateListsAsync()
        => BumpListVersionAsync();

    public async Task InvalidateAsync(int productId)
    {
        await TryRemoveAsync(BuildItemCacheKey(productId));
        await BumpListVersionAsync();
    }

    private async Task BumpListVersionAsync()
    {
        var current = await TryGetStringAsync(ListVersionKey);
        if (!current.Ok)
            return;

        var version = string.IsNullOrEmpty(current.Value) ? "1" : current.Value;
        var next = (int.TryParse(version, out var parsed) ? parsed : 1) + 1;
        await TrySetStringAsync(ListVersionKey, next.ToString(CultureInfo.InvariantCulture), options: null);
    }

    private async Task<(bool Ok, string? Value)> TryGetStringAsync(string cacheKey)
    {
        try
        {
            return (true, await _cache.GetStringAsync(cacheKey));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Product cache read failed for {CacheKey}. The request continues from the database.", cacheKey);
            return (false, null);
        }
    }

    private async Task TrySetStringAsync(string cacheKey, string value, DistributedCacheEntryOptions? options)
    {
        try
        {
            if (options is null)
                await _cache.SetStringAsync(cacheKey, value);
            else
                await _cache.SetStringAsync(cacheKey, value, options);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Product cache write failed for {CacheKey}.", cacheKey);
        }
    }

    private async Task TryRemoveAsync(string cacheKey)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Product cache removal failed for {CacheKey}.", cacheKey);
        }
    }

    private static string BuildItemCacheKey(int id) => $"products:item:{id}";

    private static string BuildListCacheKey(string version, ProductQueryParameters queryParameters)
    {
        return $"products:list:{version}:{queryParameters.PageNumber}:{queryParameters.PageSize}:{Normalize(queryParameters.Search)}:{Normalize(queryParameters.MinPrice)}:{Normalize(queryParameters.MaxPrice)}";
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "_"
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));

    private static string Normalize(decimal? value)
        => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "_";
}

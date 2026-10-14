using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Infrastructure;

public class ProductRepository : IProductRepository
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

    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ApplicationDbContext dbContext, IDistributedCache cache, ILogger<ProductRepository>? logger = null)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger ?? NullLogger<ProductRepository>.Instance;
    }

    public async Task<PageResult<Product>> GetAllAsync(ProductQueryParameters queryParameters)
    {
        var versionRead = await TryGetStringAsync(ListVersionKey);
        if (versionRead.Ok)
        {
            var version = string.IsNullOrEmpty(versionRead.Value) ? "1" : versionRead.Value;
            var cacheKey = BuildListCacheKey(version, queryParameters);
            var cachedResult = await TryGetStringAsync(cacheKey);

            if (cachedResult.Ok && !string.IsNullOrWhiteSpace(cachedResult.Value))
            {
                var cachedPage = JsonSerializer.Deserialize<PageResult<Product>>(cachedResult.Value, CacheJsonOptions);
                if (cachedPage is not null)
                    return cachedPage;
            }

            var result = await QueryPageAsync(queryParameters);
            if (cachedResult.Ok)
            {
                await TrySetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(result, CacheJsonOptions),
                    ListCacheOptions);
            }

            return result;
        }

        return await QueryPageAsync(queryParameters);
    }

    private async Task<PageResult<Product>> QueryPageAsync(ProductQueryParameters queryParameters)
    {

        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
            query = query.Where(p => p.Name.Contains(queryParameters.Search));

        if (queryParameters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= queryParameters.MinPrice.Value);

        if (queryParameters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= queryParameters.MaxPrice.Value);

        var count = await query.CountAsync();

        var items = await query.Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();

        return new PageResult<Product>
        {
            Items = items,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = count
        };
    }

    public async Task<Product?> GetByIDAsync(int id)
    {
        var cacheKey = BuildItemCacheKey(id);
        var cachedProduct = await TryGetStringAsync(cacheKey);

        if (cachedProduct.Ok && !string.IsNullOrWhiteSpace(cachedProduct.Value))
            return JsonSerializer.Deserialize<Product>(cachedProduct.Value, CacheJsonOptions);

        var product = await _dbContext.Products.FindAsync(id);

        if (product is not null && cachedProduct.Ok)
        {
            await TrySetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(product, CacheJsonOptions),
                ItemCacheOptions);
        }

        return product;
    }

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        await TrySetStringAsync(
            BuildItemCacheKey(product.ID),
            JsonSerializer.Serialize(product, CacheJsonOptions),
            ItemCacheOptions);
        await BumpListVersionAsync();
    }

    public async Task<bool> UpdateAsync(Product prod)
    {
        var stored = await _dbContext.Products.FindAsync(prod.ID);
        if (stored is null)
            return false;

        stored.Name = prod.Name;
        stored.Description = prod.Description;
        stored.Price = prod.Price;
        stored.RowVersion++;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        await TrySetStringAsync(
            BuildItemCacheKey(stored.ID),
            JsonSerializer.Serialize(stored, CacheJsonOptions),
            ItemCacheOptions);
        await BumpListVersionAsync();
        return true;
    }

    public async Task<DeleteProductResult> DeleteAsync(int id)
    {
        if (await _dbContext.UserProducts.AnyAsync(assignment => assignment.ProductID == id))
            return DeleteProductResult.Assigned;

        var product = await _dbContext.Products.FindAsync(id);
        if (product is null)
            return DeleteProductResult.NotFound;

        _dbContext.Products.Remove(product);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return DeleteProductResult.Assigned;
        }

        await TryRemoveAsync(BuildItemCacheKey(id));
        await BumpListVersionAsync();
        return DeleteProductResult.Deleted;
    }

    public async Task InvalidateCacheAsync(int productID)
    {
        await TryRemoveAsync(BuildItemCacheKey(productID));
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
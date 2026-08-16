using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
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

    public ProductRepository(ApplicationDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<PageResult<Product>> GetAllAsync(ProductQuerryParametars queryParametars)
    {
        var version = await GetListVersionAsync();
        var cacheKey = BuildListCacheKey(queryParametars);
        var cachedResult = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedResult))
        {
            var cachedPage = JsonSerializer.Deserialize<PageResult<Product>>(cachedResult, CacheJsonOptions);
            if (cachedPage is not null)
            {
                return cachedPage;
            }
        }

        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParametars.Search))
            query = query.Where(p => p.Name.Contains(queryParametars.Search));

        if (queryParametars.MinPrice.HasValue)
            query = query.Where(p => p.Price >= queryParametars.MinPrice.Value);

        if (queryParametars.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= queryParametars.MaxPrice.Value);

        var count = await query.CountAsync();

        var items = await query.Skip((queryParametars.PageNumber - 1) * queryParametars.PageSize)
            .Take(queryParametars.PageSize)
            .ToListAsync();

        var result = new PageResult<Product>
        {
            Items = items,
            PageNumber = queryParametars.PageNumber,
            PageSize = queryParametars.PageSize,
            TotalCount = count
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result, CacheJsonOptions), ListCacheOptions);

        return result;
    }

    public async Task<Product?> GetByIDAsync(int id)
    {
        var cacheKey = BuildItemCacheKey(id);
        var cachedProduct = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrWhiteSpace(cachedProduct))
        {
            return JsonSerializer.Deserialize<Product>(cachedProduct, CacheJsonOptions);
        }

        var product = await _dbContext.Products.FindAsync(id);

        if (product is not null)
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product, CacheJsonOptions), ItemCacheOptions);
        }

        return product;
    }

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();

        await _cache.SetStringAsync(BuildItemCacheKey(product.ID), JsonSerializer.Serialize(product, CacheJsonOptions), ItemCacheOptions);
        await BumpListVersionAsync();
    }

    public async Task UpdateAsync(Product prod)
    {
        _dbContext.Products.Update(prod);
        await _dbContext.SaveChangesAsync();

        await _cache.SetStringAsync(BuildItemCacheKey(prod.ID), JsonSerializer.Serialize(prod, CacheJsonOptions), ItemCacheOptions);
        await BumpListVersionAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product is null)
        {
            return;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        await _cache.RemoveAsync(BuildItemCacheKey(id));
        await BumpListVersionAsync();
    }

    public async Task InvalidateCacheAsync(int productID)
    {
        await _cache.RemoveAsync(BuildItemCacheKey(productID));
        await BumpListVersionAsync();
    }
    
    private async Task<string> GetListVersionAsync()
    {
        var version = await _cache.GetStringAsync(ListVersionKey);
        return string.IsNullOrEmpty(version) ? "1" : version;
    }

    private async Task BumpListVersionAsync()
    {
        var current = await GetListVersionAsync();
        var next = (int.TryParse(current, out var v) ? v : 1) + 1;
        await _cache.SetStringAsync(ListVersionKey, next.ToString(CultureInfo.InvariantCulture));
    }
    
    
    

    private static string BuildItemCacheKey(int id) => $"products:item:{id}";

    private static string BuildListCacheKey(ProductQuerryParametars queryParametars)
    {
        return $"products:list:{queryParametars.PageNumber}:{queryParametars.PageSize}:{Normalize(queryParametars.Search)}:{Normalize(queryParametars.MinPrice)}:{Normalize(queryParametars.MaxPrice)}";
    }
    
    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "_"
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));

    private static string Normalize(decimal? value)
        => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "_";
}
using WebAPI.Domain;

namespace WebAPI.Application;

public enum DeleteProductResult
{
    Deleted,
    NotFound,
    Assigned
}

public interface IProductRepository
{
       
    Task<PageResult<Product>> GetAllAsync(ProductQueryParameters queryParameters);
    Task<Product?> GetByIDAsync(int id);
    Task AddAsync(Product prod);
    Task<bool> UpdateAsync(Product prod);
    Task<DeleteProductResult> DeleteAsync(int id);
}
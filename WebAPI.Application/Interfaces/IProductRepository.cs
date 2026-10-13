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
       
    Task<PageResult<Product>> GetAllAsync(ProductQuerryParametars queryParametars);  
    Task<Product?> GetByIDAsync(int id); // Product? can be null 
    Task AddAsync(Product prod); // post 
    Task<bool> UpdateAsync(Product prod); // put 
    Task<DeleteProductResult> DeleteAsync(int id); // delete 

    Task InvalidateCacheAsync(int productID);


}
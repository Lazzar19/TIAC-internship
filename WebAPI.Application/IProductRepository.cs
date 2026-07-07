using WebAPI.Domain;

namespace WebAPI.Application;

public interface IProductRepository
{
       
    Task<IEnumerable<Product>> GetAllAsync(); // 
    Task<Product?> GetByIDAsync(int id); // Product? can be null 
    Task AddAsync(Product prod); // post 
    Task UpdateAsync(Product prod); // put 
    Task DeleteAsync(int id); // delete 
    
    
}
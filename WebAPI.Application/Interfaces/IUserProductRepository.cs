namespace WebAPI.Application;
using WebAPI.Domain;


public enum AssignProductResult
{
    Success,
    ProductNotFound,
    InsufficientStock
}

public interface IUserProductRepository
{
    Task<IEnumerable<UserProduct?>> GetByUserIDAsync(int userID);
    Task<UserProduct?> GetAsync(int userID, int productID);
   
    Task<AssignProductResult> AssignAsync(int userId, int productId, int quantity);
    Task DeleteAsync(UserProduct userProduct);
}
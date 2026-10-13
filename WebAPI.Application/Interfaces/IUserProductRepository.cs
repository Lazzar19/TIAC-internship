namespace WebAPI.Application;
using WebAPI.Domain;


public enum AssignProductResult
{
    Success,
    UserNotFound,
    ProductNotFound,
    InsufficientStock,
    Conflict
}

public interface IUserProductRepository
{
    Task<IEnumerable<UserProduct?>> GetByUserIDAsync(int userID);
    Task<UserProduct?> GetAsync(int userID, int productID);
   
    Task<AssignProductResult> AssignAsync(int userId, int productId, int quantity);
    Task<bool> DeleteAsync(UserProduct userProduct);
}
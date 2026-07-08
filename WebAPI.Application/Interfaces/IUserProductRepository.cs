namespace WebAPI.Application;
using WebAPI.Domain;


public interface IUserProductRepository
{
    Task<IEnumerable<UserProduct?>> GetByUserIDAsync(int userID);
    Task<UserProduct?> GetAsync(int userID, int productID);
   
    Task AddOrUpdateAsync(UserProduct userProduct);
    Task DeleteAsync(UserProduct userProduct);
}
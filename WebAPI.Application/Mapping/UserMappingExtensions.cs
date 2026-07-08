using WebAPI.Domain;
namespace WebAPI.Application;

public static class UserMappingExtensions
{
    public static UserDTO ToDTO(this User user)
    {
        return new UserDTO
        {
            ID = user.ID,
            UserName = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
        };
    }

    public static User ToEntity(this CreateUserDTO dto)
    {
        return new User
        {
           Username = dto.Username,
           Email = dto.Email,
           PasswordHash = dto.Password
            // temporarily empty, hash will come with JWT auth
        };
    }


    public static UserProductDTO ToDTO(this UserProduct userProduct)
    {
        return new UserProductDTO
        {
            UserID = userProduct.UserID,
            ProductID = userProduct.ProductID,
            Quantity = userProduct.NumberOfProducts
        };
    }

    public static UserProduct ToEntity(this AsigningUserToProductDTO dto, int userID)
    // userID must be paramter for this function, cuz we dont have userID included in AsigningUserToProductDTO class 
    // userID can be extracted from URL, thats why we pass it like parametar
    {
        return new UserProduct
        {
            UserID = userID,
            ProductID = dto.ProductID,
            NumberOfProducts = dto.Quantity
        };
    }
    
}
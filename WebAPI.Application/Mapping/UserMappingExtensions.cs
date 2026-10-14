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

    public static UserProduct ToEntity(this AssignProductToUserDTO dto, int userID)
    {
        return new UserProduct
        {
            UserID = userID,
            ProductID = dto.ProductID,
            NumberOfProducts = dto.Quantity
        };
    }
    
}
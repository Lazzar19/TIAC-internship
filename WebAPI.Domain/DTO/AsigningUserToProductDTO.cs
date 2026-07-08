namespace WebAPI.Domain;

public class AsigningUserToProductDTO
{
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    
    // user id will come from URL, something like this: /api/users/{userID}/products
    
}
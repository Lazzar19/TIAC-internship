namespace WebAPI.Domain;

public class UserProduct
{
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
    
    public int UserID { get; set; }
    public int ProductID { get; set; }
    
    public int  NumberOfProducts { get; set; }
    
    

}
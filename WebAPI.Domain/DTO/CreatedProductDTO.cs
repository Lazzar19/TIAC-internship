namespace WebAPI.Domain;

public class CreatedProductDTO
{
    // data base automatically incrementing userID  
    
    public string  Name {get; set;} = String.Empty;
    public string? Description { get; set; }
    public decimal Price {get; set;}
    public int Stock { get; set;  }
    
    //public string Code {get; set;} = String.Empty;
    
}
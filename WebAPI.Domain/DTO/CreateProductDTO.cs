namespace WebAPI.Domain;

public class CreateProductDTO
{
    public string  Name {get; set;} = String.Empty;
    public string? Description { get; set; }
    public decimal Price {get; set;}
    public int Stock { get; set;  }
}

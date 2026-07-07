namespace WebAPI.Domain;

//  Domain ← Application ← Infrastructure ← Api


public class Product
{
    public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public decimal Price  { get; set; }
    public String? Description { get; set; } = String.Empty; // String? can be nullable 
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    
    //public string Code { get; set; } = string.Empty;
    
}
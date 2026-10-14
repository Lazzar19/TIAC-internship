namespace WebAPI.Domain;

public class Product
{
    public int ID { get; set; }
    public string Name { get; set; } = String.Empty;
    public decimal Price  { get; set; }
    public String? Description { get; set; } = String.Empty;
    public int Stock { get; set; }
    public long RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    
    //public string Code { get; set; } = string.Empty;
    
}
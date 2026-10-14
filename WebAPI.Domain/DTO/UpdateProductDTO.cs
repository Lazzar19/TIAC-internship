namespace WebAPI.Domain;

public class UpdateProductDTO
{
    public string Name { get; set; } = String.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
}
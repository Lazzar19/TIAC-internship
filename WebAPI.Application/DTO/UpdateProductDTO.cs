namespace WebAPI.Application;

public class UpdateProductDTO
{
    public string Name { get; set; } = String.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
}

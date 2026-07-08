namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebAPI.Application;
using WebAPI.Domain;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductRepository productRepository_;

    public ProductController(IProductRepository productRepository)
    {
        productRepository_ = productRepository;
    }

    [HttpGet] // get all 
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAll()
    {
        var allProducts = await productRepository_.GetAllAsync();
        return Ok(allProducts.Select(p => p.ToDTO()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDTO>> GetById(int id)
    {
        Product prod = await productRepository_.GetByIDAsync(id);
        return prod is null ? NotFound() : Ok(prod.ToDTO()); // just checking if there is actually a prod
    }

    [HttpPost]
    public async Task<ActionResult<ProductDTO>> Post(CreatedProductDTO prodDTO)
    {
       var product = prodDTO.ToEntity();
        
        await productRepository_.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.ID }, product.ToDTO());
        
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, UpdateProductDTO prod)
    {
        var already_existing = await productRepository_.GetByIDAsync(id);
        if(already_existing is null) return NotFound();
        
        already_existing.Name = prod.Name;
        already_existing.Description = prod.Description;
        already_existing.Price = prod.Price;
        already_existing.Stock = prod.Stock;
        
        await productRepository_.UpdateAsync(already_existing);
        return NoContent();

    }

   

}
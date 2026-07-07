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
    public async Task<ActionResult<Product>> GetAll()
    {
        var allProducts = await productRepository_.GetAllAsync();
        return Ok(allProducts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        Product prod = await productRepository_.GetByIDAsync(id);
        return prod is null ? NotFound() : Ok(prod); // just checking if there is actually a prod
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Post(Product prod)
    {
        prod.ID = 0;
        await productRepository_.AddAsync(prod);
        return CreatedAtAction(nameof(GetById), new { id = prod.ID }, prod);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> Put(int id, Product prod)
    {
        if (id != prod.ID)
            return BadRequest();

        var existing = await productRepository_.GetByIDAsync(id);
        if (existing is null) return NotFound(); // failed to fatch

        await productRepository_.UpdateAsync(prod);
        return NoContent();

    }
    
}
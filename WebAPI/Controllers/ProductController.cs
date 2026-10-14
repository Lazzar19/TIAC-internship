using Microsoft.AspNetCore.Authorization;
using WebAPI.Authorization;

namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebAPI.Application;
using WebAPI.Domain;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductRepository productRepository_;

    public ProductController(IProductRepository productRepository)
    {
        productRepository_ = productRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PageResult<ProductDTO>>> GetAll([FromQuery] ProductQueryParameters queryParameters)
    {
        var result = await productRepository_.GetAllAsync(queryParameters);

        var DTOresult = new PageResult<ProductDTO>
        {
            Items = result.Items.Select(p => p.ToDTO()),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };

        return Ok(DTOresult);
    }
   

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDTO>> GetById(int id)
    {
        var prod = await productRepository_.GetByIDAsync(id);
        if (prod is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "Product was not found.");

        return Ok(prod.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = CurrentUser.AdminRole)]
    public async Task<ActionResult<ProductDTO>> Post(CreateProductDTO prodDTO)
    {
       var product = prodDTO.ToEntity();
        
        await productRepository_.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.ID }, product.ToDTO());
        
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = CurrentUser.AdminRole)]
    public async Task<IActionResult> Put(int id, UpdateProductDTO prod)
    {
        var already_existing = await productRepository_.GetByIDAsync(id);
        if (already_existing is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "Product was not found.");
        
        already_existing.Name = prod.Name;
        already_existing.Description = prod.Description;
        already_existing.Price = prod.Price;

        var updated = await productRepository_.UpdateAsync(already_existing);
        if (!updated)
            return this.ApiProblem(StatusCodes.Status409Conflict, "The product was updated by someone else. Retry the request.");

        return NoContent();

    }

    [HttpDelete("{id}")]
    [Authorize(Roles = CurrentUser.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await productRepository_.DeleteAsync(id);
        return result switch
        {
            DeleteProductResult.NotFound => this.ApiProblem(StatusCodes.Status404NotFound, "Product was not found."),
            DeleteProductResult.Assigned => this.ApiProblem(StatusCodes.Status409Conflict, "Product cannot be deleted while it is assigned to a user."),
            _ => NoContent()
        };
    }

}
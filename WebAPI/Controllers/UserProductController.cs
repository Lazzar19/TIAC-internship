using WebAPI.Application;

namespace WebAPI.Controllers;

using WebAPI.Infrastructure;
using WebAPI.Domain;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users/{userId}/products")]
public class UserProductController : ControllerBase
{

    private readonly IUserProductRepository userProductRepository_;

    public UserProductController(IUserProductRepository userProductRepository)
    {
        userProductRepository_ = userProductRepository;
    }


    [HttpGet]

    public async Task<ActionResult<IEnumerable<UserProductDTO>>> GetByUserIDAsync(int userID)
    {
        var items = await userProductRepository_.GetByUserIDAsync(userID);
        return Ok(items.Select(up => up.ToDTO()));
    }

    [HttpPost]

    public async Task<ActionResult<UserProductDTO>> Assign(int userID, AsigningUserToProductDTO dto)
    {
        var userProduct = new UserProduct
        {
            UserID = userID,
            ProductID = dto.ProductID,
            NumberOfProducts = dto.Quantity
        };
        
        await userProductRepository_.AddOrUpdateAsync(userProduct);

        var result = await userProductRepository_.GetAsync(userID, dto.ProductID);
        return Ok(result.ToDTO());

    }


    [HttpDelete("{productId}")]

    public async Task<IActionResult> DeleteAsync(int userID, int productID)
    {
        var existing = await userProductRepository_.GetAsync(userID, productID);
        if (existing is null) return NotFound();

        await userProductRepository_.DeleteAsync(existing);
        return NoContent();

    }
    
    
}
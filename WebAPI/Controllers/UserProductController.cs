using Microsoft.AspNetCore.Authorization;
using WebAPI.Application;

namespace WebAPI.Controllers;

using WebAPI.Infrastructure;
using WebAPI.Domain;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users/{userId}/products")]
[Authorize]
public class UserProductController : ControllerBase
{

    private readonly IUserProductRepository userProductRepository_;

    public UserProductController(IUserProductRepository userProductRepository)
    {
        userProductRepository_ = userProductRepository;
    }


    [HttpGet]

    public async Task<ActionResult<IEnumerable<UserProductDTO>>> GetByUserIDAsync(int userId)
    {
        var items = await userProductRepository_.GetByUserIDAsync(userId);
        return Ok(items.Select(up => up.ToDTO()));
    }

    [HttpPost]

    public async Task<ActionResult<UserProductDTO>> Assign(int userId, AsigningUserToProductDTO dto)
    {
        var userProduct = new UserProduct
        {
            UserID = userId,
            ProductID = dto.ProductID,
            NumberOfProducts = dto.Quantity
        };
        
        await userProductRepository_.AddOrUpdateAsync(userProduct);

        var result = await userProductRepository_.GetAsync(userId, dto.ProductID);
        return Ok(result.ToDTO());

    }


    [HttpDelete("{productId}")]

    public async Task<IActionResult> DeleteAsync(int userId, int productID)
    {
        var existing = await userProductRepository_.GetAsync(userId, productID);
        if (existing is null) return NotFound();

        await userProductRepository_.DeleteAsync(existing);
        return NoContent();

    }
    
    
}
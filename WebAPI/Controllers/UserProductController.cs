using Microsoft.AspNetCore.Authorization;
using WebAPI.Application;
using WebAPI.Authorization;

namespace WebAPI.Controllers;

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
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var items = await userProductRepository_.GetByUserIDAsync(userId);
        return Ok(items.Select(up => up.ToDTO()));
    }

    /// <summary>
    /// Sets the total quantity of this product owned by the user.
    /// Quantity replaces the previous total. It is not added to it.
    /// </summary>
    [HttpPost]
    [EndpointDescription("Quantity is the new total owned by this user, not an increment. Sending 2 after 5 sets the owned quantity to 2 and returns 3 units to stock.")]
    public async Task<ActionResult<UserProductDTO>> Assign(int userId, AsigningUserToProductDTO dto)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var result = await userProductRepository_.AssignAsync(userId, dto.ProductID, dto.Quantity);

        switch (result)
        {
            case AssignProductResult.UserNotFound:
                return NotFound($"User with ID: {userId} cannot be found");
            case AssignProductResult.ProductNotFound:
                return NotFound($"Product with ID: {dto.ProductID} cannot be found");
            case AssignProductResult.InsufficientStock:
                return BadRequest($"Product with ID: {dto.ProductID} has insufficient stock");
            case AssignProductResult.Conflict:
                return Conflict("The product changed while assigning. Retry the request.");
        }
        var userProduct = await userProductRepository_.GetAsync(userId, dto.ProductID);
        return Ok(userProduct!.ToDTO());
    }


    [HttpDelete("{productId}")]

    public async Task<IActionResult> DeleteAsync(int userId, int productID)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var existing = await userProductRepository_.GetAsync(userId, productID);
        if (existing is null) return NotFound();

        var deleted = await userProductRepository_.DeleteAsync(existing);
        if (!deleted) return NotFound();

        return NoContent();

    }
    
    
}
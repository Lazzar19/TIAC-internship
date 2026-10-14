using Microsoft.AspNetCore.Authorization;
using WebAPI.Application;
using WebAPI.Authorization;

namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users/{userId}/products")]
[Authorize]
public class UserProductController : ControllerBase
{

    private readonly AssignmentService assignmentService_;

    public UserProductController(AssignmentService assignmentService)
    {
        assignmentService_ = assignmentService;
    }


    [HttpGet]

    public async Task<ActionResult<IEnumerable<UserProductDTO>>> GetByUserIDAsync(int userId)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var items = await assignmentService_.GetByUserIdAsync(userId);
        return Ok(items.Select(up => up.ToDTO()));
    }

    /// <summary>
    /// Sets the total quantity of this product owned by the user.
    /// Quantity replaces the previous total. It is not added to it.
    /// </summary>
    [HttpPost]
    [EndpointDescription("Quantity is the new total owned by this user, not an increment. Sending 2 after 5 sets the owned quantity to 2 and returns 3 units to stock.")]
    public async Task<ActionResult<UserProductDTO>> Assign(int userId, AssignProductToUserDTO dto)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var result = await assignmentService_.AssignAsync(userId, dto.ProductID, dto.Quantity);

        switch (result)
        {
            case AssignProductResult.UserNotFound:
                return this.ApiProblem(StatusCodes.Status404NotFound, $"User with ID: {userId} cannot be found");
            case AssignProductResult.ProductNotFound:
                return this.ApiProblem(StatusCodes.Status404NotFound, $"Product with ID: {dto.ProductID} cannot be found");
            case AssignProductResult.InsufficientStock:
                return this.ApiProblem(StatusCodes.Status400BadRequest, $"Product with ID: {dto.ProductID} has insufficient stock");
            case AssignProductResult.Conflict:
                return this.ApiProblem(StatusCodes.Status409Conflict, "The product changed while assigning. Retry the request.");
        }
        var userProduct = await assignmentService_.GetAsync(userId, dto.ProductID);
        return Ok(userProduct!.ToDTO());
    }


    [HttpDelete("{productId}")]

    public async Task<IActionResult> DeleteAsync(int userId, int productID)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(userId);
        if (denied is not null)
            return denied;

        var existing = await assignmentService_.GetAsync(userId, productID);
        if (existing is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "Assignment was not found.");

        var deleted = await assignmentService_.UnassignAsync(existing);
        if (!deleted)
            return this.ApiProblem(StatusCodes.Status404NotFound, "Assignment was not found.");

        return NoContent();

    }
    
    
}
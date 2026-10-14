using Microsoft.AspNetCore.Authorization;
using WebAPI.Application;
using WebAPI.Application.Interfaces;
using WebAPI.Authorization;

namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebAPI.Domain;


[ApiController]
[Route("api/[controller]")]
[Authorize]

public class UserController : ControllerBase
{

    private readonly IUserRepository userRepository_;
    private readonly IPasswordHasher passwordHasher_;
    private readonly SessionService sessionService_;
    private readonly UserAccountService userAccountService_;

    public UserController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        SessionService sessionService,
        UserAccountService userAccountService)
    {
        userRepository_ = userRepository;
        passwordHasher_ = passwordHasher;
        sessionService_ = sessionService;
        userAccountService_ = userAccountService;
    }


    [HttpGet]
    [Authorize(Roles = CurrentUser.AdminRole)]
    public async Task<ActionResult<PageResult<UserDTO>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var users = await userRepository_.GetAllAsync(pageNumber, pageSize);
        return Ok(new PageResult<UserDTO>
        {
            Items = users.Items.Select(user => user.ToDTO()),
            PageNumber = users.PageNumber,
            PageSize = users.PageSize,
            TotalCount = users.TotalCount
        });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDTO>> Me()
    {
        if (!CurrentUser.TryGetId(User, out var userId))
            return this.ApiProblem(StatusCodes.Status401Unauthorized, "Authentication is required.");

        var user = await userRepository_.GetByIDAsync(userId);
        if (user is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "User was not found.");

        return Ok(user.ToDTO());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDTO>> GetById(int id)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(id);
        if (denied is not null)
            return denied;

        var user = await userRepository_.GetByIDAsync(id);
        if (user is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "User was not found.");

        return Ok(user.ToDTO());
    }

    [HttpPost]
    [Authorize(Roles = CurrentUser.AdminRole)]
    public async Task<ActionResult<UserDTO>> Post(CreateUserDTO dto)
    {
        if (await userRepository_.EmailExistsAsync(dto.Email))
        {
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");
        }

        var newUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHasher_.Hash(dto.Password) 
        };

        try
        {
            await userRepository_.AddAsync(newUser);
        }
        catch (EmailAlreadyExistsException)
        {
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");
        }

        return CreatedAtAction(nameof(GetById), new { id = newUser.ID }, newUser.ToDTO());
    }
    
    
    [HttpPut("{id}")]

    public async Task<IActionResult> Put(int id, UpdateUserDTO dto)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(id);
        if (denied is not null)
            return denied;

        var alreadyExisting = await userRepository_.GetByIDAsync(id);
        if (alreadyExisting is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "User was not found.");

        if (!string.Equals(alreadyExisting.Email, dto.Email, StringComparison.OrdinalIgnoreCase) &&
            await userRepository_.EmailExistsAsync(dto.Email))
        {
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");
        }

        alreadyExisting.Username = dto.UserName;
        alreadyExisting.Email = dto.Email;

        try
        {
            await userRepository_.UpdateAsync(alreadyExisting);
        }
        catch (EmailAlreadyExistsException)
        {
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");
        }

        return NoContent();
    }

    
    [HttpPut("{id}/password")]

    public async Task<IActionResult> ChangePasswordAsync(int id, ChangePasswordDTO dto)
    {
        var denied = this.DenyUnlessOwnerOrAdmin(id);
        if (denied is not null)
            return denied;

        var user = await userRepository_.GetByIDAsync(id);
        if (user is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "User was not found.");

        if (!passwordHasher_.Verify(user.PasswordHash, dto.CurrentPassword))
            return this.ApiProblem(StatusCodes.Status400BadRequest, "Current password is incorrect");
        
        user.PasswordHash = passwordHasher_.Hash(dto.NewPassword);
        
        await userRepository_.UpdateAsync(user);
        await sessionService_.RevokeAsync(id);
        return NoContent();
    }
    
    
    [HttpDelete("{id}")]
    [Authorize(Roles = CurrentUser.AdminRole)] 

    public async Task<IActionResult> DeleteAsync(int id)
    {
        var user = await userRepository_.GetByIDAsync(id);
        if (user is null)
            return this.ApiProblem(StatusCodes.Status404NotFound, "User was not found.");

        await userAccountService_.DeleteAsync(user);
        return NoContent();
    }
    
}
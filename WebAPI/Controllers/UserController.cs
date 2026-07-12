using WebAPI.Application;
using WebAPI.Application.Validators;

namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebAPI.Domain;
using WebAPI.Infrastructure;


[ApiController]
[Route("api/[controller]")]

public class UserController : ControllerBase
{

    private readonly IUserRepository userRepository_;

    public UserController(IUserRepository userRepository)
    {
        userRepository_ = userRepository;
    }


    [HttpGet]

    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllAsync()
    {
        var users = await userRepository_.GetAllAsync();
        return Ok(users.Select(u => u.ToDTO()));
    }

    [HttpGet("{id}")]

    public async Task<ActionResult<UserDTO>> GetAsync(int id)
    {
        var user = await userRepository_.GetByIDAsync(id);
        return user is null ? NotFound() : Ok(user.ToDTO());
    }

    [HttpPost]

    public async Task<ActionResult<UserDTO>> AddAsync(CreateUserDTO dto)
    {
        var newUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = dto.Password // just plain text, JWT will do the hashing 
        };

        await userRepository_.AddAsync(newUser);
        return CreatedAtAction(nameof(GetAsync), new { id = newUser.ID }, newUser);
    }

    [HttpPut("{id}")]

    public async Task<IActionResult> UpdateAsync(int id, UpdateUserDTO dto)
    {
        var alreadyExisting = await userRepository_.GetByIDAsync(id);
        if (alreadyExisting is null) return NotFound();

        alreadyExisting.Username = dto.UserName;
        alreadyExisting.Email = dto.Email;
        
        await  userRepository_.UpdateAsync(alreadyExisting);
        return NoContent();
    }

    [HttpDelete("{id}")]

    public async Task<IActionResult> DeleteAsync(int id)
    {
        var user = await userRepository_.GetByIDAsync(id);
        if (user is null) return NotFound();

        await userRepository_.DeleteAsync(user);
        return NoContent();
    }
    
}
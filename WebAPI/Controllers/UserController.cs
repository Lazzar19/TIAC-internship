using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WebAPI.Application;
using WebAPI.Application.Validators;
using WebAPI.Application.Interfaces;

namespace WebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using WebAPI.Domain;
using WebAPI.Infrastructure;


[ApiController]
[Route("api/[controller]")]
[Authorize]

public class UserController : ControllerBase
{

    private readonly IUserRepository userRepository_;
    private readonly IPasswordHasher passwordHasher_;
    
    public UserController(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        userRepository_ = userRepository;
        passwordHasher_ = passwordHasher;
    }


    [HttpGet]

    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAll()
    {
        var users = await userRepository_.GetAllAsync();
        return Ok(users.Select(u => u.ToDTO()));
    }

    [HttpGet("{id}")]

    public async Task<ActionResult<UserDTO>> GetById(int id)
    {
        var user = await userRepository_.GetByIDAsync(id);
        return user is null ? NotFound() : Ok(user.ToDTO());
    }

    [HttpPost]
    public async Task<ActionResult<UserDTO>> Post(CreateUserDTO dto)
    {
        var newUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHasher_.Hash(dto.Password) 
        };

        await userRepository_.AddAsync(newUser);
        return CreatedAtAction(nameof(GetById), new { id = newUser.ID }, newUser.ToDTO());
    }
    
    
    [HttpPut("{id}")]

    public async Task<IActionResult> Put(int id, UpdateUserDTO dto)
    {
        var alreadyExisting = await userRepository_.GetByIDAsync(id);
        if (alreadyExisting is null) return NotFound();

        alreadyExisting.Username = dto.UserName;
        alreadyExisting.Email = dto.Email;
        
        await  userRepository_.UpdateAsync(alreadyExisting);
        return NoContent();
    }

    
    [HttpPut("{id}/password")]

    public async Task<IActionResult> ChangePasswordAsync(int id, ChangePasswordDTO dto)
    {
        var user = await userRepository_.GetByIDAsync(id);
        if (user is null) return NotFound();

        if (!passwordHasher_.Verify(user.PasswordHash, dto.CurrentPassword))
        {
            return BadRequest("Current password is incorrect");
        }
        
        user.PasswordHash = passwordHasher_.Hash(dto.NewPassword);
        
        await  userRepository_.UpdateAsync(user);
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
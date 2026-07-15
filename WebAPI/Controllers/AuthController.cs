using Microsoft.AspNetCore.Mvc;
using WebAPI.Application.Interfaces;
using WebAPI.Application.Validators;


namespace WebAPI.Controllers;

using WebAPI.Domain;
using WebAPI.Application;

[ApiController]
[Route("api/[controller]")]


public class AuthController : ControllerBase
{
    private readonly IUserRepository userRepository_;
    private readonly IPasswordHasher passwordHasher_;
    private readonly ITokenService tokenService_;


    public AuthController(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        userRepository_ = userRepository;
        passwordHasher_ = passwordHasher;
        tokenService_ = tokenService;
    }


    [HttpPost("register")]

    public async Task<ActionResult<UserDTO>> Register(CreateUserDTO dto)
    {
        var user =  new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHasher_.Hash(dto.Password)
        };

        await userRepository_.AddAsync(user);
        return Ok(user.ToDTO());

    }

    [HttpPost("login")]

    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
    {
        var allUsers_ = await userRepository_.GetAllAsync();
        var user_ = allUsers_.FirstOrDefault(u => u.Email == dto.Email);
        
        if (user_ == null || !passwordHasher_.Verify(user_.PasswordHash, dto.Password))
            return Unauthorized("Invalid email or password");

        var token_ = tokenService_.GenerateToken(user_);
        return Ok(new AuthResponseDTO { Token = token_, Username = user_.Username });
    }
    
    


}
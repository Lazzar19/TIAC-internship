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
    private readonly IRefreshTokenRepository  refreshTokenRepository_;


    public AuthController(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
    {
        userRepository_ = userRepository;
        passwordHasher_ = passwordHasher;
        tokenService_ = tokenService;
        refreshTokenRepository_ = refreshTokenRepository;
    }


    [HttpPost("register")]

    public async Task<ActionResult<UserDTO>> Register(CreateUserDTO dto)
    {
        if (await userRepository_.EmailExistsAsync(dto.Email))
        {
            return Conflict("Email already exists.");
        }

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
        var refreshTokenValue = tokenService_.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user_.ID,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        
        await refreshTokenRepository_.AddAsync(refreshToken);

        return Ok(new AuthResponseDTO
        {
            Token = token_,
            RefreshToken = refreshTokenValue,
            Username = user_.Username,
        });

    }


    [HttpPost("refresh")]

    public async Task<ActionResult<AuthResponseDTO>> Refresh(RefreshRequestDTO dto)
    {
        
        var storedToken = await refreshTokenRepository_.GetByTokenAsync(dto.RefreshToken);

        if (storedToken is null || storedToken.isRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token");

        var newToken = tokenService_.GenerateToken(storedToken.User);

        return Ok(new AuthResponseDTO
        {
            Token = newToken,
            RefreshToken = storedToken.Token,
            Username = storedToken.User.Username
        });

    }
    
    


}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("auth")]

    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
    {
        var user_ = await userRepository_.GetByEmailAsync(dto.Email);
        
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
    [EnableRateLimiting("auth")]

    public async Task<ActionResult<AuthResponseDTO>> Refresh(RefreshRequestDTO dto)
    {
        var storedToken = await refreshTokenRepository_.GetByTokenAsync(dto.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.IsExpired)
            return Unauthorized("Invalid or expired refresh token");

        // Token rotation: revoke old token
        await refreshTokenRepository_.RevokeAsync(storedToken);

        // Generate new token pair
        var newToken = tokenService_.GenerateToken(storedToken.User);
        var newRefreshTokenValue = tokenService_.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        await refreshTokenRepository_.AddAsync(newRefreshToken);

        return Ok(new AuthResponseDTO
        {
            Token = newToken,
            RefreshToken = newRefreshTokenValue,
            Username = storedToken.User.Username
        });
    }

    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
        
        if (userId == 0)
            return BadRequest("Invalid user token");

        // Revoke all refresh tokens for this user
        await refreshTokenRepository_.RevokeAllByUserIdAsync(userId);

        return NoContent();
    }
    
    


}
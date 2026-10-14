using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Interfaces;
using WebAPI.Application.Validators;
using WebAPI.Authorization;


namespace WebAPI.Controllers;

using WebAPI.Domain;
using WebAPI.Application;
using WebAPI.Infrastructure;

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
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserDTO>> Register(CreateUserDTO dto)
    {
        if (await userRepository_.EmailExistsAsync(dto.Email))
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");

        var user =  new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHasher_.Hash(dto.Password)
        };

        try
        {
            await userRepository_.AddAsync(user);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolation.Is(exception))
        {
            return this.ApiProblem(StatusCodes.Status409Conflict, "Email already exists.");
        }

        return CreatedAtAction(
            nameof(UserController.GetById),
            "User",
            new { id = user.ID },
            user.ToDTO());

    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]

    public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
    {
        var user_ = await userRepository_.GetByEmailAsync(dto.Email);
        
        if (user_ == null || !passwordHasher_.Verify(user_.PasswordHash, dto.Password))
            return this.ApiProblem(StatusCodes.Status401Unauthorized, "Invalid email or password");

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
        var newRefreshTokenValue = tokenService_.GenerateRefreshToken();
        var rotation = await refreshTokenRepository_.RotateAsync(dto.RefreshToken, newRefreshTokenValue, DateTime.UtcNow);

        if (!rotation.Succeeded || rotation.User is null)
            return this.ApiProblem(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token");

        return Ok(new AuthResponseDTO
        {
            Token = tokenService_.GenerateToken(rotation.User),
            RefreshToken = newRefreshTokenValue,
            Username = rotation.User.Username
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (!CurrentUser.TryGetId(User, out var userId))
            return this.ApiProblem(StatusCodes.Status401Unauthorized, "Authentication is required.");

        await refreshTokenRepository_.RevokeAllByUserIdAsync(userId);

        return NoContent();
    }
    
    


}
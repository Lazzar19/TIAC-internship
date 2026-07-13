using Microsoft.IdentityModel.Tokens;
using WebAPI.Application.Interfaces;

namespace WebAPI.Infrastructure;

using WebAPI.Domain;
using WebAPI.Application;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.IdentityModel.Tokens;


public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    
    public TokenService(IConfiguration conf)
    {
        _configuration = conf;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.ID.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token_ = new JwtSecurityToken(issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: cred);
        
        return new JwtSecurityTokenHandler().WriteToken(token_);

    }
    
}
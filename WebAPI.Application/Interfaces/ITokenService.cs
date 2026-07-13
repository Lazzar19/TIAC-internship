using WebAPI.Domain;

namespace WebAPI.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
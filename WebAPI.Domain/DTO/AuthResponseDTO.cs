namespace WebAPI.Domain;

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
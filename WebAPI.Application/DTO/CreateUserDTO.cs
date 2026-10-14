namespace WebAPI.Application;

public class CreateUserDTO
{
    public string Username { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string Password { get; set; } = String.Empty;
}

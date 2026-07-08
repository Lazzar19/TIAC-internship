namespace WebAPI.Domain;

public class UserDTO
{
    // never return password hash
        
    public int ID { get; set; }
    public string UserName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
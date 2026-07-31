namespace WebAPI.Domain;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool isRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
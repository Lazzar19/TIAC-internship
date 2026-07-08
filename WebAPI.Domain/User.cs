namespace WebAPI.Domain;

public class User
{
    public int ID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash {get; set;}  = string.Empty;
    public string Email {get; set;} =  string.Empty;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    
    public ICollection<Product> UserProducts {get; set;} = new List<Product>();
    
    
}
namespace WebAPI.Domain;

public class ChangePasswordDTO
{
    // like we said, instead of also chaning password while we doing some other changes on email or username, we 
    // create separate class for changing password 
    
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
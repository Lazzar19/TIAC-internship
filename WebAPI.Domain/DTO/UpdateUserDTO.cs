namespace WebAPI.Domain;

public class UpdateUserDTO
{
    public string UserName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    
    // instead of Password field in UpdateUserDTO, there is separate class ChangePasswordDTo 
    // if we used field for password everytime when someone wants to change userName or email he needs to 
    // enter the old password / empty string or change it to new one 
    
    // this approach is much more batter for JWT authentication later 
    
}
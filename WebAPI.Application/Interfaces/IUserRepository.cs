namespace WebAPI.Application.Validators;
using WebAPI.Domain;

public interface IUserRepository
{
    Task<PageResult<User>> GetAllAsync(int pageNumber, int pageSize);
    Task<User?> GetByIDAsync(int userID);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

}
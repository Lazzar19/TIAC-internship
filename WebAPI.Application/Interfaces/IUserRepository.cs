using WebAPI.Application;
using WebAPI.Domain;

namespace WebAPI.Application.Interfaces;

public interface IUserRepository
{
    Task<PageResult<User>> GetAllAsync(int pageNumber, int pageSize);
    Task<User?> GetByIDAsync(int userID);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<IReadOnlyList<int>> DeleteAsync(User user);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

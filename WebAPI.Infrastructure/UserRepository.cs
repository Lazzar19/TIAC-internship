using Microsoft.EntityFrameworkCore;
using WebAPI.Application.Validators;

namespace WebAPI.Infrastructure;

using WebAPI.Application;
using WebAPI.Domain;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext dbContext_;
    
    public UserRepository(ApplicationDbContext dbContext) { dbContext_ = dbContext; }


    public async Task<IEnumerable<User>> GetAllAsync() => await dbContext_.Users.ToListAsync();
    public async Task<User?> GetByIDAsync(int userID) =>  await dbContext_.Users.FindAsync(userID);

    public async Task AddAsync(User user)
    {
        await dbContext_.Users.AddAsync(user);
        await dbContext_.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        dbContext_.Users.Update(user);
        await dbContext_.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        dbContext_.Users.Remove(user);
        await dbContext_.SaveChangesAsync();
    }
    


}
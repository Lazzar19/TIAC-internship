using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;
namespace WebAPI.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProduct> UserProducts => Set<UserProduct>();
    
    

}
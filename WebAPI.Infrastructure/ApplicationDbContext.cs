using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;
namespace WebAPI.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();

}
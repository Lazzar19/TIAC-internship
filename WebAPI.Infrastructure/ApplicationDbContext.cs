using Microsoft.EntityFrameworkCore;
using WebAPI.Domain;
namespace WebAPI.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProduct> UserProducts => Set<UserProduct>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<UserProduct>(entity =>
        {
            entity.HasKey(up => new { up.UserID, up.ProductID });   

            entity.HasOne(up => up.User)
                .WithMany(u => u.UserProducts)
                .HasForeignKey(up => up.UserID);

            entity.HasOne(up => up.Product)
                .WithMany()
                .HasForeignKey(up => up.ProductID);
        });
    }
    
    

}
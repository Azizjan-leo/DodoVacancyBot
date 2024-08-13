using Microsoft.EntityFrameworkCore;

namespace Console.Advanced.Data;
public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<City>()
            .HasData(new City { Id = 1, Name = "Бишкек" });
    }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<City> Cities { get; set; }
}

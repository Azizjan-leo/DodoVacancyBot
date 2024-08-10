using Microsoft.EntityFrameworkCore;

namespace Console.Advanced.Data;
public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users { get; set; }
}

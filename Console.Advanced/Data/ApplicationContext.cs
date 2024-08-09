using Microsoft.EntityFrameworkCore;

namespace Console.Advanced.Data;
internal class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users { get; set; }
}

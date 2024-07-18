using Microsoft.EntityFrameworkCore;

namespace Webhook.Controllers.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       // modelBuilder.HasPostgresExtension("uuid-ossp");
    }
}
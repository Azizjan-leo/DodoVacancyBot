using Microsoft.EntityFrameworkCore;
using Webhook.Controllers.Data.Entities;

namespace Webhook.Controllers.Data;

public sealed class ApplicationDbContext : DbContext 
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Vacancy> Vacancies { get; set; }
    public DbSet<LangVacancy> LangVacancies { get; set; }
}

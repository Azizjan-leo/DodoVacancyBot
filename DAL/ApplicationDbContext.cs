using Microsoft.EntityFrameworkCore;
using DAL.Enitities;

namespace DAL;

public sealed class ApplicationDbContext : DbContext 
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Vacancy> Vacancies { get; set; }
    public DbSet<LangVacancy> LangVacancies { get; set; }
}

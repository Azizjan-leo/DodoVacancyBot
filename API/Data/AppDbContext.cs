using Core.Data;
using Microsoft.EntityFrameworkCore;


namespace API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>()
                .HasData(new City { Id = 1, Name = "Бишкек" });

            modelBuilder.Entity<Position>()
                 .HasData(new Position { Id = 1, RuName = "Пиццамейкер", KyName = "Пиццамейкер" });
            modelBuilder.Entity<Position>()
                 .HasData(new Position { Id = 2, RuName = "Кассир", KyName = "Кассир" });
            modelBuilder.Entity<Position>()
                 .HasData(new Position { Id = 3, RuName = "Курьер", KyName = "Курьер" });
            modelBuilder.Entity<Position>()
                 .HasData(new Position { Id = 4, RuName = "Клинер", KyName = "Клинер" });


        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Vacancy> Vacancies { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<AppFIll> AppFIlls { get; set; }

    }
}

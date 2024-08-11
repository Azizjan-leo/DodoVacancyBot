using Microsoft.EntityFrameworkCore;

namespace Console.Advanced.Data;
public class MyDbContextFactory
{
    private readonly string _connectionString;

    public MyDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ApplicationDBContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
        optionsBuilder.UseNpgsql(_connectionString);

        return new ApplicationDBContext(optionsBuilder.Options);
    }
}
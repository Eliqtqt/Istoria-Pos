using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CafeWebsite.Data
{
    public class CafeDbContextFactory : IDesignTimeDbContextFactory<CafeDbContext>
    {
        public CafeDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                                   "Host=localhost;Port=5432;Database=IstoriaCoffeeDb;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<CafeDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new CafeDbContext(optionsBuilder.Options);
        }
    }
}
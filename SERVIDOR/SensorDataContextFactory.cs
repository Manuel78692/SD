using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SERVIDOR.Data;

namespace SERVIDOR
{
    /// <summary>
    /// Design-time factory for creating SensorDataContext instances
    /// Used by EF Core tools for migrations
    /// </summary>
    public class SensorDataContextFactory : IDesignTimeDbContextFactory<SensorDataContext>
    {
        public SensorDataContext CreateDbContext(string[] args)
        {
            Console.WriteLine("Creating DbContext for design-time operations...");
            
            var optionsBuilder = new DbContextOptionsBuilder<SensorDataContext>();
            var connectionString = DatabaseConfig.GetConnectionString();
            
            Console.WriteLine($"Using connection string: {connectionString}");
            optionsBuilder.UseSqlite(connectionString);

            return new SensorDataContext(optionsBuilder.Options);
        }
    }
}

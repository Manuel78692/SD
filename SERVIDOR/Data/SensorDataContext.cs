using Microsoft.EntityFrameworkCore;
using SERVIDOR.Models;

namespace SERVIDOR.Data
{
    /// <summary>
    /// Database context for sensor data storage
    /// Optimized for HPC analysis with separate tables per sensor type
    /// </summary>
    public class SensorDataContext : DbContext
    {
        public DbSet<GpsReading> GpsReadings { get; set; }
        public DbSet<TemperatureReading> TemperatureReadings { get; set; }
        public DbSet<GyroReading> GyroReadings { get; set; }
        public DbSet<HumidityReading> HumidityReadings { get; set; }
        public DbSet<PhReading> PhReadings { get; set; }

        public SensorDataContext(DbContextOptions<SensorDataContext> options) : base(options)
        {
        }        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Table Per Type (TPT) strategy for separate tables per sensor type
            // This is optimal for HPC analysis as each sensor type gets its own table
            modelBuilder.Entity<GpsReading>().ToTable("GpsReadings");
            modelBuilder.Entity<TemperatureReading>().ToTable("TemperatureReadings");
            modelBuilder.Entity<GyroReading>().ToTable("GyroReadings");
            modelBuilder.Entity<HumidityReading>().ToTable("HumidityReadings");
            modelBuilder.Entity<PhReading>().ToTable("PhReadings");

            // Configure base SensorReading entity
            modelBuilder.Entity<SensorReading>()
                .HasIndex(s => s.Timestamp)
                .HasDatabaseName("IX_SensorReading_Timestamp");

            modelBuilder.Entity<SensorReading>()
                .HasIndex(s => s.WavyId)
                .HasDatabaseName("IX_SensorReading_WavyId");

            modelBuilder.Entity<SensorReading>()
                .HasIndex(s => new { s.WavyId, s.Timestamp })
                .HasDatabaseName("IX_SensorReading_WavyId_Timestamp");

            // Configure specific sensor entities with optimized indexes for HPC queries
            ConfigureGpsReading(modelBuilder);
            ConfigureTemperatureReading(modelBuilder);
            ConfigureGyroReading(modelBuilder);
            ConfigureHumidityReading(modelBuilder);
            ConfigurePhReading(modelBuilder);
        }

        private static void ConfigureGpsReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GpsReading>()
                .HasIndex(g => new { g.Latitude, g.Longitude })
                .HasDatabaseName("IX_GpsReading_Location");

            modelBuilder.Entity<GpsReading>()
                .Property(g => g.Latitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<GpsReading>()
                .Property(g => g.Longitude)
                .HasPrecision(10, 7);

            modelBuilder.Entity<GpsReading>()
                .Property(g => g.Altitude)
                .HasPrecision(8, 2);
        }

        private static void ConfigureTemperatureReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TemperatureReading>()
                .HasIndex(t => t.Value)
                .HasDatabaseName("IX_TemperatureReading_Value");

            modelBuilder.Entity<TemperatureReading>()
                .Property(t => t.Value)
                .HasPrecision(5, 2);
        }

        private static void ConfigureGyroReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GyroReading>()
                .Property(g => g.X)
                .HasPrecision(8, 4);

            modelBuilder.Entity<GyroReading>()
                .Property(g => g.Y)
                .HasPrecision(8, 4);

            modelBuilder.Entity<GyroReading>()
                .Property(g => g.Z)
                .HasPrecision(8, 4);
        }

        private static void ConfigureHumidityReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HumidityReading>()
                .HasIndex(h => h.Value)
                .HasDatabaseName("IX_HumidityReading_Value");

            modelBuilder.Entity<HumidityReading>()
                .Property(h => h.Value)
                .HasPrecision(5, 2);
        }

        private static void ConfigurePhReading(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PhReading>()
                .HasIndex(p => p.Value)
                .HasDatabaseName("IX_PhReading_Value");

            modelBuilder.Entity<PhReading>()
                .Property(p => p.Value)
                .HasPrecision(4, 2);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SERVIDOR.Data;

namespace SERVIDOR
{
    /// <summary>
    /// Simple tool to verify database content
    /// </summary>
    public class DatabaseVerifier
    {
        public static async Task VerifyDatabaseContent()
        {
            var optionsBuilder = new DbContextOptionsBuilder<SensorDataContext>();
            optionsBuilder.UseSqlite(DatabaseConfig.GetConnectionString());
            
            using var context = new SensorDataContext(optionsBuilder.Options);
            
            Console.WriteLine("=== DATABASE CONTENT VERIFICATION ===\n");
            
            // Check GPS readings
            var gpsCount = await context.GpsReadings.CountAsync();
            Console.WriteLine($"GPS Readings: {gpsCount}");
            if (gpsCount > 0)
            {
                var lastGps = await context.GpsReadings
                    .OrderByDescending(g => g.Timestamp)
                    .FirstOrDefaultAsync();
                Console.WriteLine($"  Latest: {lastGps?.WavyId} - {lastGps?.Latitude},{lastGps?.Longitude} at {lastGps?.Timestamp}");
            }
            
            // Check Temperature readings
            var tempCount = await context.TemperatureReadings.CountAsync();
            Console.WriteLine($"Temperature Readings: {tempCount}");
            if (tempCount > 0)
            {
                var lastTemp = await context.TemperatureReadings
                    .OrderByDescending(t => t.Timestamp)
                    .FirstOrDefaultAsync();
                Console.WriteLine($"  Latest: {lastTemp?.WavyId} - {lastTemp?.Value}°C at {lastTemp?.Timestamp}");
            }
            
            // Check other sensor types
            var gyroCount = await context.GyroReadings.CountAsync();
            var humidityCount = await context.HumidityReadings.CountAsync();
            var phCount = await context.PhReadings.CountAsync();
            
            Console.WriteLine($"Gyro Readings: {gyroCount}");
            Console.WriteLine($"Humidity Readings: {humidityCount}");
            Console.WriteLine($"pH Readings: {phCount}");
            
            Console.WriteLine($"\nTotal sensor readings in database: {gpsCount + tempCount + gyroCount + humidityCount + phCount}");
        }
    }
}

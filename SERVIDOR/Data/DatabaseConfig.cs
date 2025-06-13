using System.IO;

namespace SERVIDOR.Data
{
    /// <summary>
    /// Configuration settings for the database
    /// </summary>
    public static class DatabaseConfig
    {        /// <summary>
        /// Gets the SQLite connection string for the sensor data database
        /// </summary>
        public static string GetConnectionString()
        {
            var databasePath = Path.Combine("..", "SERVIDOR", "dados", "sensor_data.db");
            
            // Ensure the dados directory exists
            var dadosDirectory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(dadosDirectory) && !Directory.Exists(dadosDirectory))
            {
                Directory.CreateDirectory(dadosDirectory);
            }

            return $"Data Source={databasePath}";
        }
    }
}

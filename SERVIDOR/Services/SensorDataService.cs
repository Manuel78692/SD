using Microsoft.EntityFrameworkCore;
using SERVIDOR.Data;
using SERVIDOR.Models;

namespace SERVIDOR.Services
{    /// <summary>
    /// Service for handling sensor data operations with the database
    /// Replaces CSV file operations with database storage
    /// </summary>
    public class SensorDataService
    {
        private readonly SensorDataContext _context;
        private readonly Action<string> _logger;        public SensorDataService(Action<string>? logger = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SensorDataContext>();
            optionsBuilder.UseSqlite(DatabaseConfig.GetConnectionString());
            _context = new SensorDataContext(optionsBuilder.Options);
            _logger = logger ?? Console.WriteLine; // Fallback to Console.WriteLine if no logger provided
            
            // Ensure database and tables are created
            try
            {
                _context.Database.EnsureCreated();
                _logger("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves a block of sensor data to the appropriate table based on sensor type
        /// </summary>
        /// <param name="dataBlock">Array of data lines in format "WAVY_ID:data:timestamp"</param>
        /// <param name="sensorType">Type of sensor data (gps, temperatura, gyro, humidade, ph)</param>
        public async Task SaveSensorDataAsync(string[] dataBlock, string sensorType)
        {
            try
            {
                var readings = ParseDataBlock(dataBlock, sensorType);
                if (readings.Any())
                {
                    _logger($"Parsed {readings.Count} {sensorType} readings, attempting database save...");
                    await _context.AddRangeAsync(readings);
                    await _context.SaveChangesAsync();

                    _logger($"Successfully saved {readings.Count} {sensorType} readings to database.");
                }
                else
                {
                    _logger($"No valid {sensorType} readings found in data block.");
                }
            }            catch (Exception ex)
            {
                _logger($"Error saving {sensorType} data to database: {ex.Message}");
                throw; // Re-throw so caller can handle fallback if needed
            }
        }        /// <summary>
        /// Parses timestamp in format: 2025-01-01-00-00-30 (YYYY-MM-DD-HH-MM-SS)
        /// </summary>
        private bool TryParseCustomTimestamp(string timestampStr, out DateTime timestamp)
        {
            timestamp = default;
            
            if (string.IsNullOrWhiteSpace(timestampStr))
                return false;

            // Try standard DateTime parsing first
            if (DateTime.TryParse(timestampStr, out timestamp))
                return true;

            // Handle custom format: 2025-01-01-00-00-30
            var parts = timestampStr.Split('-');
            if (parts.Length == 6 &&
                int.TryParse(parts[0], out int year) &&
                int.TryParse(parts[1], out int month) &&
                int.TryParse(parts[2], out int day) &&
                int.TryParse(parts[3], out int hour) &&
                int.TryParse(parts[4], out int minute) &&
                int.TryParse(parts[5], out int second))
            {
                try
                {
                    timestamp = new DateTime(year, month, day, hour, minute, second);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses data block and creates appropriate sensor reading entities
        /// </summary>
        private List<SensorReading> ParseDataBlock(string[] dataBlock, string sensorType)
        {
            var readings = new List<SensorReading>();

            foreach (var line in dataBlock)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(':');
                if (parts.Length < 3) continue;                var wavyId = parts[0];
                var data = parts[1];
                var timestampStr = parts[2];

                // Handle the specific timestamp format: 2025-01-01-00-00-30
                if (!TryParseCustomTimestamp(timestampStr, out var timestamp))
                {
                    Console.WriteLine($"Invalid timestamp format: {timestampStr}");
                    continue;
                }

                var reading = CreateSensorReading(sensorType, wavyId, data, timestamp);
                if (reading != null)
                {
                    readings.Add(reading);
                }
            }

            return readings;
        }

        /// <summary>
        /// Creates the appropriate sensor reading entity based on type
        /// </summary>
        private SensorReading? CreateSensorReading(string sensorType, string wavyId, string data, DateTime timestamp)
        {
            return sensorType.ToLower() switch
            {
                "gps" => CreateGpsReading(wavyId, data, timestamp),
                "temperatura" => CreateTemperatureReading(wavyId, data, timestamp),
                "gyro" => CreateGyroReading(wavyId, data, timestamp),
                "humidade" => CreateHumidityReading(wavyId, data, timestamp),
                "ph" => CreatePhReading(wavyId, data, timestamp),
                _ => null
            };
        }        private GpsReading? CreateGpsReading(string wavyId, string data, DateTime timestamp)
        {
            // Expected format: "lat,lng,alt" or "lat,lng"
            var coords = data.Split(',');
            if (coords.Length < 2) return null;

            if (double.TryParse(coords[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) && 
                double.TryParse(coords[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
            {
                var reading = new GpsReading
                {
                    WavyId = wavyId,
                    Timestamp = timestamp,
                    Latitude = lat,
                    Longitude = lng
                };

                if (coords.Length > 2 && double.TryParse(coords[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var alt))
                {
                    reading.Altitude = alt;
                }

                return reading;
            }

            return null;
        }private TemperatureReading? CreateTemperatureReading(string wavyId, string data, DateTime timestamp)
        {
            if (double.TryParse(data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return new TemperatureReading
                {
                    WavyId = wavyId,
                    Timestamp = timestamp,
                    Value = value
                };
            }
            return null;
        }        private GyroReading? CreateGyroReading(string wavyId, string data, DateTime timestamp)
        {
            // Expected format: "x,y,z"
            var values = data.Split(',');
            if (values.Length == 3 &&
                double.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var y) &&
                double.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var z))
            {
                return new GyroReading
                {
                    WavyId = wavyId,
                    Timestamp = timestamp,
                    X = x,
                    Y = y,
                    Z = z
                };
            }
            return null;
        }        private HumidityReading? CreateHumidityReading(string wavyId, string data, DateTime timestamp)
        {
            if (double.TryParse(data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return new HumidityReading
                {
                    WavyId = wavyId,
                    Timestamp = timestamp,
                    Value = value
                };
            }
            return null;
        }        private PhReading? CreatePhReading(string wavyId, string data, DateTime timestamp)
        {
            if (double.TryParse(data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return new PhReading
                {
                    WavyId = wavyId,
                    Timestamp = timestamp,
                    Value = value
                };
            }
            return null;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

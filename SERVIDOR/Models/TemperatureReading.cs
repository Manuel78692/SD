using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Represents temperature sensor readings
    /// </summary>
    public class TemperatureReading : SensorReading
    {
        [Required]
        public double Value { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; } = "°C";

        public TemperatureReading()
        {
            SensorType = "temperatura";
        }
    }
}

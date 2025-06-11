using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Represents gyroscope sensor readings with X, Y, Z axis values
    /// </summary>
    public class GyroReading : SensorReading
    {
        [Required]
        public double X { get; set; }

        [Required]
        public double Y { get; set; }

        [Required]
        public double Z { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; } = "deg/s";

        public GyroReading()
        {
            SensorType = "gyro";
        }
    }
}

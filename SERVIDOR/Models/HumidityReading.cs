using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Represents humidity sensor readings
    /// </summary>
    public class HumidityReading : SensorReading
    {
        [Required]
        public double Value { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; } = "%";

        public HumidityReading()
        {
            SensorType = "humidade";
        }
    }
}

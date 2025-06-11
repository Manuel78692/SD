using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Represents pH sensor readings
    /// </summary>
    public class PhReading : SensorReading
    {
        [Required]
        public double Value { get; set; }

        [MaxLength(10)]
        public string Unit { get; set; } = "pH";

        public PhReading()
        {
            SensorType = "ph";
        }
    }
}

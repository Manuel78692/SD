using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Represents GPS sensor readings with latitude, longitude and altitude
    /// </summary>
    public class GpsReading : SensorReading
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public double? Altitude { get; set; }

        public GpsReading()
        {
            SensorType = "gps";
        }
    }
}

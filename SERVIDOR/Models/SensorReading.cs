using System.ComponentModel.DataAnnotations;

namespace SERVIDOR.Models
{
    /// <summary>
    /// Base class for all sensor readings with common properties
    /// </summary>
    public abstract class SensorReading
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string WavyId { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(20)]
        public string SensorType { get; set; } = string.Empty;
    }
}

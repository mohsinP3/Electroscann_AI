using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class Review : BaseEntity
    {
        public int ReviewerId { get; set; }

        public User? Reviewer { get; set; }

        public int ElectricianId { get; set; }

        public Electrician? Electrician { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
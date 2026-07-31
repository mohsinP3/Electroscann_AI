using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class JobApplication : BaseEntity
    {
        public int JobId { get; set; }

        public Job? Job { get; set; }

        public int ElectricianId { get; set; }

        public Electrician? Electrician { get; set; }

        public string CoverLetter { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
using System.ComponentModel.DataAnnotations;

namespace ElectroScanAI.Models.Entities
{
    public class WireCalculation : BaseEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        public double Load { get; set; }

        public double Distance { get; set; }

        [MaxLength(50)]
        public string WireSize { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;
    }
}